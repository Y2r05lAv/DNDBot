using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.AttackSystem
{
    class AttackRealisation : BotBodyRealisation
    {
        private readonly ILogger<AttackRealisation> _logger;
        private readonly HealCommands _healCommands;
        private readonly DamageTask _damageTask;
        private readonly AttackTask _attackTask;

        public AttackRealisation(ILogger<AttackRealisation> logger, HealCommands healCommands, DamageTask damageTask, AttackTask attackTask)
        {
            _logger = logger;
            _healCommands = healCommands;
            _damageTask = damageTask;
            _attackTask = attackTask;
        }

        public async Task AttackRealisationHandle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            if (TemporaryHpState == "ToAdd")
            {
                await HandleTemporaryHpInput(botClient, message, cancellationToken);
                return;
            }

            if (TemporaryHpReductionState == "ToReduce")
            {
                await HandleTemporaryHpReductionInput(botClient, message, cancellationToken);
                return;
            }

            if (HealState == "ToHeal")
            {
                await _healCommands.ToHealPlayer(botClient, cancellationToken, message);
                return;
            }

            if (userStateAtack[0] == "ToDamage")
            {
                await _damageTask.Damage(botClient, cancellationToken, message, damage);
                return;
            }

            if (userStateAtack[0] == "ToAttack")
                await _attackTask.Attack(botClient, cancellationToken, message, choosedUser);
        }

        public async Task HandleAttackCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery?.Data == null)
                return;

            if (callbackQuery.Message?.Chat == null)
                return;

            if (await HandleTemporaryHealthCallback(botClient, callbackQuery, cancellationToken))
                return;

            if (!callbackQuery.Data.StartsWith("attack_") && !callbackQuery.Data.StartsWith("heal_"))
                return;

            if (!TryParseAttackOrHealTargetCallback(callbackQuery.Data, out var targetId, out var targetCharacterId))
                return;

            bool isCombatActive = global::DNDBot.ComandsRealisation.IsCombatSessionActive(callbackQuery.Message.Chat.Id);
            if (isCombatActive && !InlineKeyboardsClass.IsAllowedCombatTarget(targetId, targetCharacterId))
            {
                try
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        "Ціль не є учасником поточного бою.",
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to answer non-combat target callback. CallbackId={CallbackId}, Data={CallbackData}, UserId={UserId}",
                        callbackQuery.Id,
                        callbackQuery.Data,
                        callbackQuery.From?.Id);
                }

                return;
            }

            if (!isCombatActive && !IsAllowedGeneralTarget(targetId, targetCharacterId))
            {
                try
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        "Ціль більше недоступна.",
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to answer unavailable target callback. CallbackId={CallbackId}, Data={CallbackData}, UserId={UserId}",
                        callbackQuery.Id,
                        callbackQuery.Data,
                        callbackQuery.From?.Id);
                }

                return;
            }

            damagedCreature = targetId;
            damagedCreatureCharacterId = targetCharacterId;

            if (damagedCreatureCharacterId > 0)
                FightCharacter[damagedCreature] = damagedCreatureCharacterId;

            if (callbackQuery.Data.StartsWith("heal_"))
                await HealCallBackHandle(botClient, callbackQuery.Message, callbackQuery.Message.Chat.Id, cancellationToken);
            else
                await AttackCallBackHandle(botClient, callbackQuery.Message.Chat.Id, cancellationToken);
        }

        private static bool TryParseAttackOrHealTargetCallback(string data, out long id, out int characterId)
        {
            id = 0;
            characterId = 0;

            var parts = data.Split('_');
            if (parts.Length < 2)
                return false;

            if (parts[0] != "attack" && parts[0] != "heal")
                return false;

            if (!long.TryParse(parts[1], out id))
                return false;

            if (parts.Length >= 3)
                int.TryParse(parts[2], out characterId);

            return true;
        }

        private static bool TryParseExtendedTargetCallback(string data, string callbackPrefix, out long id, out int characterId)
        {
            id = 0;
            characterId = 0;

            if (!data.StartsWith(callbackPrefix + "_", StringComparison.Ordinal))
                return false;

            var parts = data.Split('_');
            if (parts.Length < 4)
                return false;

            if (!long.TryParse(parts[^2], out id))
                return false;

            return int.TryParse(parts[^1], out characterId);
        }

        private static bool IsAllowedGeneralTarget(long id, int characterId)
        {
            if (characterId > 0)
                return UserData.CheckifExist(id, characterId);

            return Monsters.Any(m => m != null && m.ID == id);
        }

        private async Task HealCallBackHandle(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Started healing task: Healing Started");

            string userName = damagedCreatureCharacterId > 0
                ? UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name")
                : (Monsters.FirstOrDefault(m => m.ID == damagedCreature)?.Name ?? damagedCreature.ToString());

            await botClient.SendTextMessageAsync(chatId, $"На скільки ви хочете вилікувати ціль '{userName}'?", cancellationToken: cancellationToken);

            HealState = "ToHeal";
            await _healCommands.ToHealPlayer(botClient, cancellationToken, message);
        }

        private async Task AttackCallBackHandle(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (Monsters.Count <= 0)
                if (damagedCreature < 1000)
                    return;

            _logger.LogInformation("Started attack task: Attack Started. Damaged Creature: {Creature}", damagedCreature);

            string monsterName = "";
            string playerName = "";

            if (damagedCreature < 1000)
            {
                var monster = Monsters.FirstOrDefault(m => m != null && m.ID == damagedCreature);

                if (monster != null)
                    monsterName = monster.Name;
                else
                {
                    _logger.LogWarning("Monster with ID {MonsterId} not found.", damagedCreature);
                    monsterName = "Невідомий монстр";
                }
            }
            else
            {
                playerName = damagedCreatureCharacterId > 0
                    ? UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name")
                    : UserData.DataBaseValueReader<string>(damagedCreature, "Name");
            }

            string name = damagedCreature > 999 ? playerName : monsterName;

            _logger.LogInformation("Player name: {PlayerName}; MonsterName: {MonsterName}", playerName, monsterName);

            await botClient.SendTextMessageAsync(chatId, $"На скільки ви хочете атакувати '{name}'?", cancellationToken: cancellationToken);

            choosedUser = damagedCreature;
            userStateAtack[0] = "ToAttack";
        }

        private async Task<bool> HandleTemporaryHealthCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            string data = callbackQuery.Data ?? string.Empty;
            long chatId = callbackQuery.Message?.Chat?.Id ?? callbackQuery.From.Id;

            if (data == "thp_menu_add")
            {
                await ShowUserTargetSelection(
                    botClient,
                    chatId,
                    "Оберіть гравця, кому додати тимчасові хіти:",
                    "thp_add",
                    cancellationToken);
                return true;
            }

            if (data == "thp_menu_clear")
            {
                await ShowUserTargetSelection(
                    botClient,
                    chatId,
                    "Оберіть гравця, у кого скинути тимчасові хіти:",
                    "thp_clear",
                    cancellationToken);
                return true;
            }

            if (data == "hpred_menu_add")
            {
                await ShowUserTargetSelection(
                    botClient,
                    chatId,
                    "Оберіть гравця, кому тимчасово знизити поточне і максимальне HP:",
                    "hpred_add",
                    cancellationToken);
                return true;
            }

            if (data == "hpred_menu_reset")
            {
                await ShowUserTargetSelection(
                    botClient,
                    chatId,
                    "Оберіть гравця, у кого скинути тимчасове зниження HP:",
                    "hpred_reset",
                    cancellationToken);
                return true;
            }

            if (TryParseExtendedTargetCallback(data, "thp_add", out var targetId, out var targetCharacterId))
            {
                if (!TrySetPlayerTarget(targetId, targetCharacterId, out var userName))
                {
                    await botClient.SendTextMessageAsync(chatId, "Немає даних про цього гравця.", cancellationToken: cancellationToken);
                    return true;
                }

                await botClient.SendTextMessageAsync(chatId, $"На скільки додати тимчасові хіти гравцю '{userName}'?", cancellationToken: cancellationToken);
                TemporaryHpReductionState = "";
                HealState = "";
                userStateAtack[0] = "";
                TemporaryHpState = "ToAdd";
                return true;
            }

            if (TryParseExtendedTargetCallback(data, "thp_clear", out targetId, out targetCharacterId))
            {
                if (!TrySetPlayerTarget(targetId, targetCharacterId, out var userName))
                {
                    await botClient.SendTextMessageAsync(chatId, "Немає даних про цього гравця.", cancellationToken: cancellationToken);
                    return true;
                }

                var clearResult = UserData.ClearTemporaryHp(targetId, targetCharacterId);
                string healthDisplay = UserData.BuildHealthDisplay(clearResult.CurrentHp, clearResult.MaxHp, clearResult.TempHp);

                await botClient.SendTextMessageAsync(
                    chatId,
                    clearResult.Removed > 0
                        ? $"Тимчасові хіти у '{userName}' скинуто (було {clearResult.Removed}). Поточне здоров'я: {healthDisplay}."
                        : $"У '{userName}' не було тимчасових хітів. Поточне здоров'я: {healthDisplay}.",
                    cancellationToken: cancellationToken);
                return true;
            }

            if (TryParseExtendedTargetCallback(data, "hpred_add", out targetId, out targetCharacterId))
            {
                if (!TrySetPlayerTarget(targetId, targetCharacterId, out var userName))
                {
                    await botClient.SendTextMessageAsync(chatId, "Немає даних про цього гравця.", cancellationToken: cancellationToken);
                    return true;
                }

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"На скільки тимчасово знизити поточне і максимальне HP для '{userName}'?",
                    cancellationToken: cancellationToken);
                TemporaryHpState = "";
                HealState = "";
                userStateAtack[0] = "";
                TemporaryHpReductionState = "ToReduce";
                return true;
            }

            if (TryParseExtendedTargetCallback(data, "hpred_reset", out targetId, out targetCharacterId))
            {
                if (!TrySetPlayerTarget(targetId, targetCharacterId, out var userName))
                {
                    await botClient.SendTextMessageAsync(chatId, "Немає даних про цього гравця.", cancellationToken: cancellationToken);
                    return true;
                }

                var resetResult = UserData.ResetTemporaryMaxHpReduction(targetId, targetCharacterId);
                string healthDisplay = UserData.BuildHealthDisplay(
                    resetResult.CurrentHp,
                    resetResult.MaxHp,
                    UserData.DataBaseValueReader<int>(targetId, targetCharacterId, "TempHP"));

                await botClient.SendTextMessageAsync(
                    chatId,
                    resetResult.Restored > 0
                        ? $"Тимчасове зниження HP у '{userName}' скинуто (+{resetResult.Restored}). Поточне здоров'я: {healthDisplay}."
                        : $"У '{userName}' немає тимчасового зниження HP. Поточне здоров'я: {healthDisplay}.",
                    cancellationToken: cancellationToken);
                return true;
            }

            return false;
        }

        private async Task ShowUserTargetSelection(
            ITelegramBotClient botClient,
            long chatId,
            string messageText,
            string callbackPrefix,
            CancellationToken cancellationToken)
        {
            var targetsExist = UserData.GetUsersFromDatabase().Any(user => user.Id != masterID);
            if (!targetsExist)
            {
                await botClient.SendTextMessageAsync(chatId, "Немає доступних цілей.", cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId,
                messageText,
                replyMarkup: InlineKeyboardsClass.InlineUserTargetButtons(callbackPrefix),
                cancellationToken: cancellationToken);
        }

        private bool TrySetPlayerTarget(long targetId, int targetCharacterId, out string userName)
        {
            userName = string.Empty;

            if (targetCharacterId <= 0 || !UserData.CheckifExist(targetId, targetCharacterId))
                return false;

            damagedCreature = targetId;
            damagedCreatureCharacterId = targetCharacterId;
            FightCharacter[damagedCreature] = damagedCreatureCharacterId;

            userName = UserData.DataBaseValueReader<string>(targetId, targetCharacterId, "Name");
            return true;
        }

        private async Task HandleTemporaryHpInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.Text == null)
                return;

            if (!int.TryParse(message.Text.Trim(), out int amount) || amount <= 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть додатне число для тимчасових хітів.", cancellationToken: cancellationToken);
                return;
            }

            if (!UserData.CheckifExist(damagedCreature, damagedCreatureCharacterId))
            {
                TemporaryHpState = "";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Гравця не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            string name = UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name");
            var addResult = UserData.AddTemporaryHp(damagedCreature, damagedCreatureCharacterId, amount);
            string healthDisplay = UserData.BuildHealthDisplay(addResult.CurrentHp, addResult.MaxHp, addResult.TempHp);

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Гравцю {name} додано {addResult.Added} тимчасових хітів. Поточне здоров'я: {healthDisplay}.",
                cancellationToken: cancellationToken);

            TemporaryHpState = "";
        }

        private async Task HandleTemporaryHpReductionInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.Text == null)
                return;

            if (!int.TryParse(message.Text.Trim(), out int amount) || amount <= 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть додатне число для зниження HP.", cancellationToken: cancellationToken);
                return;
            }

            if (!UserData.CheckifExist(damagedCreature, damagedCreatureCharacterId))
            {
                TemporaryHpReductionState = "";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Гравця не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            string name = UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name");
            var reduceResult = UserData.ApplyTemporaryMaxHpReduction(damagedCreature, damagedCreatureCharacterId, amount);
            int tempHp = UserData.DataBaseValueReader<int>(damagedCreature, damagedCreatureCharacterId, "TempHP");
            string healthDisplay = UserData.BuildHealthDisplay(reduceResult.CurrentHp, reduceResult.MaxHp, tempHp);

            if (reduceResult.AppliedReduction <= 0)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Для {name} більше не можна зменшити HP (мінімальний MaxHP вже досягнуто).",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Для {name} тимчасово знижено HP на {reduceResult.AppliedReduction}. Загальне зниження: -{reduceResult.TotalReduction}. Поточне здоров'я: {healthDisplay}.",
                    cancellationToken: cancellationToken);
            }

            TemporaryHpReductionState = "";
        }
    }
}

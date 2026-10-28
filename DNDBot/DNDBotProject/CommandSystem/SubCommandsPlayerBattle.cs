using DNDBot.BotMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using DNDBot.BotSystem;
using Telegram.Bot.Types.Enums;

namespace DNDBot.CommandSystem
{
    class SubCommandsPlayerBattle : BotBodyRealisation
    {
        private readonly ILogger<SubCommandsPlayerBattle> _logger;

        public SubCommandsPlayerBattle(ILogger<SubCommandsPlayerBattle> logger)
        {
            _logger = logger;
        }

        public async Task HitCheck(ITelegramBotClient botClient, CancellationToken cancellationToken, Message msg)
        {
            if(msg?.From == null)
                return;

            long userId = msg.From.Id;
            _logger.LogInformation("Starting hit check for user {UserId}", userId);

            if (!await CheckUserExists(botClient, userId, cancellationToken)) return;

            try {
                int ModifierStrenght = UserData.DataBaseValueReader<int>(userId, "Strenght");
                int ModifierAgility = UserData.DataBaseValueReader<int>(userId, "Agility");
                int UsingModifier = ModifierStrenght > ModifierAgility ? ModifierStrenght : ModifierAgility;

                int CheckDice = MathfDND.DiceReturn(20);
                _logger.LogInformation("Hit check result for user {UserId}: Modifier={Modifier}, Dice={Dice}, Total={Total}", 
                    userId, UsingModifier, CheckDice, CheckDice + UsingModifier);
                
                await botClient.SendTextMessageAsync(msg.Chat.Id, 
                    $"Модифікатор - {UsingModifier} \nНа кубіку - {CheckDice} \nПеревірка '{msg.Text}' - {CheckDice + UsingModifier}", 
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hit check for user {UserId}", userId);
                await botClient.SendTextMessageAsync(msg.Chat.Id, 
                    "Сталася помилка під час перевірки удару. Спробуйте ще раз.", 
                    cancellationToken: cancellationToken);
            }
        }

        public async Task PlayerInitiative(ITelegramBotClient botClient, CancellationToken cancellationToken, Message msg)
        {
            if (msg?.From == null) return;

            long userId = msg.From.Id;
            _logger.LogInformation("Calculating initiative for user {UserId}", userId);

            if (!await CheckUserExists(botClient, userId, cancellationToken)) return;

            try
            {
                int characterId = UserData.GetActiveCharacterId(userId);  
                if (characterId <= 0 || !UserData.CheckifExist(userId, characterId))
                {
                    await botClient.SendTextMessageAsync(
                        msg.Chat.Id,
                        "У тебе не вибрана активна анкета. Обери персонажа /switch або створіть анкету.",
                        cancellationToken: cancellationToken);
                    return;
                }

                int speed = UserData.DataBaseValueReader<int>(userId, characterId, "Agility");
                int diceRoll = MathfDND.DiceReturn(20);
                int totalInitiative = diceRoll + speed;

                string name = UserData.DataBaseValueReader<string>(userId, characterId, "Name");

                 
                InitiativeUsers[(userId, characterId)] = new Dictionary<string, int>
        {
            { name, totalInitiative }
        };

                _logger.LogInformation("Initiative calculated for user {UserId} C{CharId}: Speed={Speed}, Dice={Dice}, Total={Total}",
                    userId, characterId, speed, diceRoll, totalInitiative);

                string response = $"Персонаж: {name} (C{characterId})\nМодифікатор: {speed}\nНа кубіку: {diceRoll}\nПеревірка '{msg.Text}': {totalInitiative}";
                await botClient.SendTextMessageAsync(msg.Chat.Id, response, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating initiative for user {UserId}", userId);
                await botClient.SendTextMessageAsync(
                    msg.Chat.Id,
                    "Сталася помилка під час розрахунку ініціативи. Спробуйте ще раз.",
                    cancellationToken: cancellationToken);
            }
        }


        public async Task MonstersList(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
        {
            _logger.LogInformation("Requesting monsters list for user {UserId} Monster count : {count}", chatId, Monsters.Count);

            if (Monsters.Count == 0)
            {
                _logger.LogWarning("No monsters found for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, "Сутностей поки немає!", cancellationToken: cancellationToken);
                return;
            }

            try
            {
                StringBuilder messageBuilder = new StringBuilder();
                foreach (var monster in Monsters)
                {
                    string attackText = monster.HasAttack()
                        ? $"{(string.IsNullOrWhiteSpace(monster.AttackName) ? "Атака" : monster.AttackName)} ({monster.AttackDamageDice})"
                        : "немає";

                    messageBuilder.AppendLine($"ID: {monster.ID}");
                    messageBuilder.AppendLine($"Ім'я: {monster.Name}");
                    messageBuilder.AppendLine($"ХП: {monster.HP}/{monster.GetEffectiveMaxHP()}");
                    messageBuilder.AppendLine($"Клас обладунків: {monster.Armour}");
                    messageBuilder.AppendLine($"Кількість досвіду: {monster.XP}");
                    messageBuilder.AppendLine($"Ініціатива: {monster.Initiative}");
                    messageBuilder.AppendLine($"Атака: {attackText}");
                    messageBuilder.AppendLine();
                }

                _logger.LogInformation("Returning monsters list for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, messageBuilder.ToString(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monsters list for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, 
                    "Сталася помилка під час отримання списку сутностей. Спробуйте ще раз.", 
                    cancellationToken: cancellationToken);
            }
        }

        public async Task RescueFromSpells(ITelegramBotClient botClient, CancellationToken cancellationToken, Message msg)
        {
            if(msg?.From == null)
                return;

            long userId = msg.From.Id;
            _logger.LogInformation("Calculating spell rescue for user {UserId}", userId);

            if (!await CheckUserExists(botClient, userId, cancellationToken)) return;

            try
            {
                int strength = UserData.DataBaseValueReader<int>(userId, "Stamina");
                int basicValueForRecueFromSpell = 8;
                int total = basicValueForRecueFromSpell + strength;

                _logger.LogInformation("Spell rescue calculated for user {UserId}: Strength={Strength}, Total={Total}", 
                    userId, strength, total);
                
                await botClient.SendTextMessageAsync(msg.Chat.Id, 
                    $"Рятунок від заклинань {total}", 
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating spell rescue for user {UserId}", userId);
                await botClient.SendTextMessageAsync(msg.Chat.Id, 
                    "Сталася помилка під час розрахунку рятунку від заклинань. Спробуйте ще раз.", 
                    cancellationToken: cancellationToken);
            }
        }

        public async Task WeaponList(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
        {
            _logger.LogInformation("Requesting weapon list for user {UserId}", chatId);

            if(chatId <= 0)
            {
                _logger.LogWarning("Invalid chat ID for weapon list request");
                return;
            }

            try
            {
                string WeaponString = string.Join(", ", Weapon);
                _logger.LogInformation("Returning weapon list for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, 
                    $"Вся доступна зброя - {WeaponString}", 
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weapon list for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, 
                    "Сталася помилка під час отримання списку зброї. Спробуйте ще раз.", 
                    cancellationToken: cancellationToken);
            }
        }

        private async Task<bool> CheckUserExists(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!UserData.CheckifExist(userId))
            {
                _logger.LogWarning("User {UserId} does not exist in the database", userId);
                await botClient.SendTextMessageAsync(userId, 
                    $"Ваших даних немає.", 
                    cancellationToken: cancellationToken);
                return false;
            }
            return true;
        }

        public async Task HealOrAttackTask(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message, bool AttackTask)
        {
            if(message?.From == null)
                return;

            long userId = message.From.Id;
            _logger.LogInformation("Starting {Action} task for user {UserId}", 
                AttackTask ? "attack" : "heal", userId);

            try
            {
                string action = AttackTask ? "атакувати" : "вилікувати";
                _logger.LogInformation("Creating inline buttons for {Action} task for user {UserId}", 
                    action, userId);

                bool isCombatActive = ComandsRealisation.IsCombatSessionActive(message.Chat.Id);
                int targetsCount;
                var keyboard = isCombatActive
                    ? InlineKeyboardsClass.InlineAttackButtons(AttackTask, out targetsCount)
                    : InlineKeyboardsClass.InlineAttackButtonsAllTargets(AttackTask, out targetsCount);

                if (targetsCount == 0)
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        isCombatActive
                            ? "Немає учасників поточного бою. Спочатку гравці мають кинути ініціативу, а майстер запустити бій."
                            : "Немає доступних цілей для цієї дії.",
                        cancellationToken: cancellationToken);
                    return;
                }
                
                await botClient.SendTextMessageAsync(message.Chat.Id, 
                    $"Кого ви хочете {action}?", 
                    parseMode: ParseMode.MarkdownV2, 
                    cancellationToken: cancellationToken, 
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting {Action} task for user {UserId}", 
                    AttackTask ? "attack" : "heal", userId);
                await botClient.SendTextMessageAsync(message.Chat.Id, 
                    $"Сталася помилка під час ініціалізації {(AttackTask ? "атаки" : "лікування")}. Спробуйте ще раз.", 
                    cancellationToken: cancellationToken);
            }
        }

        public async Task TemporaryHpTask(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
                return;

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Оберіть дію для тимчасових хітів:",
                replyMarkup: InlineKeyboardsClass.TemporaryHpActionsKeyboard(),
                cancellationToken: cancellationToken);
        }

        public async Task TemporaryHpReductionTask(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
                return;

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Оберіть дію для тимчасового зниження HP:",
                replyMarkup: InlineKeyboardsClass.TemporaryHpReductionActionsKeyboard(),
                cancellationToken: cancellationToken);
        }
    }
}

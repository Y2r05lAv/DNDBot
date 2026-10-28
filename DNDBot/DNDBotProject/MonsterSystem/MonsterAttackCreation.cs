using DNDBot.BotSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.MonsterSystem
{
    internal static class MonsterAttackCreation
    {
        private static readonly Dictionary<long, string> DraftAttackName = new();
        private static readonly Dictionary<long, List<string>> DraftDamageParts = new();
        private static readonly Dictionary<long, int> SelectedDie = new();

        public static async Task ShowHasAttackPrompt(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "Чи є у монстра атака?",
                replyMarkup: InlineKeyboardsClass.MonsterHasAttackKeyboard(),
                cancellationToken: cancellationToken);

            BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackPrompt";
        }

        public static async Task<bool> HandleAttackCallback(
            ITelegramBotClient botClient,
            long stateOwnerId,
            long chatId,
            string data,
            CancellationToken cancellationToken)
        {
            if (!data.StartsWith("monster_attack_"))
                return false;

            if (BotBodyRealisation.newMonster == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Помилка: монстра не знайдено.", cancellationToken: cancellationToken);
                return true;
            }

            if (data == "monster_attack_yes")
            {
                ResetDraft(stateOwnerId);
                await botClient.SendTextMessageAsync(chatId, "Введіть назву атаки монстра:", cancellationToken: cancellationToken);
                BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackName";
                return true;
            }

            if (data == "monster_attack_no")
            {
                BotBodyRealisation.newMonster.AttackName = "";
                BotBodyRealisation.newMonster.AttackDamageDice = "";
                ResetDraft(stateOwnerId);
                await ArmorMonsterCreation.ShowMonsterSummaryAndActions(botClient, chatId, cancellationToken);
                return true;
            }

            if (data.StartsWith("monster_attack_dice_"))
            {
                if (!DraftAttackName.ContainsKey(stateOwnerId))
                {
                    await botClient.SendTextMessageAsync(chatId, "Спершу введіть назву атаки.", cancellationToken: cancellationToken);
                    BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackName";
                    return true;
                }

                string sidesPart = data["monster_attack_dice_".Length..];
                if (!int.TryParse(sidesPart, out int dieSides) || dieSides <= 1)
                {
                    await botClient.SendTextMessageAsync(chatId, "Некоректний кубик.", cancellationToken: cancellationToken);
                    return true;
                }

                SelectedDie[stateOwnerId] = dieSides;
                BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackCount";
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Обрано d{dieSides}. Введіть кількість кубиків (ціле число > 0):",
                    cancellationToken: cancellationToken);
                return true;
            }

            if (data == "monster_attack_add_more")
            {
                if (!DraftAttackName.ContainsKey(stateOwnerId))
                {
                    await botClient.SendTextMessageAsync(chatId, "Спершу введіть назву атаки.", cancellationToken: cancellationToken);
                    BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackName";
                    return true;
                }

                BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackDiceSelect";
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Виберіть наступний кубик:",
                    replyMarkup: InlineKeyboardsClass.MonsterAttackDiceKeyboard(),
                    cancellationToken: cancellationToken);
                return true;
            }

            if (data == "monster_attack_done")
            {
                if (!DraftDamageParts.TryGetValue(stateOwnerId, out var parts) || parts.Count == 0)
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Додайте хоча б один кубик урону або оберіть 'Ні' на кроці атаки.",
                        cancellationToken: cancellationToken);
                    return true;
                }

                BotBodyRealisation.newMonster.AttackName = DraftAttackName[stateOwnerId];
                BotBodyRealisation.newMonster.AttackDamageDice = string.Join(" + ", parts);

                ResetDraft(stateOwnerId);
                await ArmorMonsterCreation.ShowMonsterSummaryAndActions(botClient, chatId, cancellationToken);
                return true;
            }

            return true;
        }

        public static async Task HandleAttackNameInput(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            if (message?.From == null || message.Text == null)
                return;

            long userId = message.From.Id;
            string attackName = message.Text.Trim();

            if (string.IsNullOrWhiteSpace(attackName))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Назва атаки не може бути порожньою.", cancellationToken: cancellationToken);
                return;
            }

            DraftAttackName[userId] = attackName;
            DraftDamageParts[userId] = new List<string>();
            SelectedDie.Remove(userId);

            BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackDiceSelect";
            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Виберіть кубик для урону:",
                replyMarkup: InlineKeyboardsClass.MonsterAttackDiceKeyboard(),
                cancellationToken: cancellationToken);
        }

        public static async Task HandleAttackCountInput(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            if (message?.From == null || message.Text == null)
                return;

            long userId = message.From.Id;

            if (!DraftAttackName.ContainsKey(userId))
            {
                BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackName";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Спершу введіть назву атаки.", cancellationToken: cancellationToken);
                return;
            }

            if (!SelectedDie.TryGetValue(userId, out int dieSides))
            {
                BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackDiceSelect";
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Спершу оберіть кубик.",
                    replyMarkup: InlineKeyboardsClass.MonsterAttackDiceKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(message.Text.Trim(), out int count) || count <= 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть коректне число кубиків (> 0).", cancellationToken: cancellationToken);
                return;
            }

            if (count > 200)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Забагато кубиків (максимум 200).", cancellationToken: cancellationToken);
                return;
            }

            if (!DraftDamageParts.TryGetValue(userId, out var parts))
            {
                parts = new List<string>();
                DraftDamageParts[userId] = parts;
            }

            parts.Add($"{count}d{dieSides}");

            string currentDamage = string.Join(" + ", parts);
            BotBodyRealisation.userState[BotBodyRealisation.MonsterCount] = "AttackDiceSelect";
            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Додано: {count}d{dieSides}\nПоточний урон: {currentDamage}\nДодати ще?",
                replyMarkup: InlineKeyboardsClass.MonsterAttackContinueKeyboard(),
                cancellationToken: cancellationToken);
        }

        private static void ResetDraft(long userId)
        {
            DraftAttackName.Remove(userId);
            DraftDamageParts.Remove(userId);
            SelectedDie.Remove(userId);
        }
    }
}

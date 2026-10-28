using DNDBot.BotMath;
using DNDBot.BotSystem;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.MonsterSystem
{
    class ArmorMonsterCreation : MonsterRealisation
    {
        public static async Task<ReplyKeyboardMarkup> ArmorVoid(
            ITelegramBotClient botClient,
            Message message,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation,
            CancellationToken cancellationToken)
        {
            long userId = message.Chat.Id;

            if (!int.TryParse(message.Text, out int armor))
            {
                var errorMsg = await botClient.SendTextMessageAsync(
                    userId,
                    "Будь ласка, напишіть цифрове значення для параметру.",
                    cancellationToken: cancellationToken);

                OldMessages.TryAdd(masterID, [message.MessageId]);
                OldMessages[userId].Add(errorMsg.MessageId);
                return replyKeyboard;
            }

            var monster = newMonster;
            monster.Armour = armor;
            monster.Initiative = MathfDND.DiceReturn(20);
            if (monster.MaxHP <= 0)
                monster.MaxHP = monster.HP;

            OldMessages.TryAdd(masterID, [message.MessageId]);
            DeleteAllOldMessages(botClient, cancellationToken, userId);
            await MonsterAttackCreation.ShowHasAttackPrompt(botClient, userId, cancellationToken);
            userState[MonsterCount] = "AttackPrompt";
            State = 0;

            return replyKeyboard;
        }

        public static async Task ShowMonsterSummaryAndActions(
            ITelegramBotClient botClient,
            long userId,
            CancellationToken cancellationToken)
        {
            var monster = newMonster;
            string attackLine = monster.HasAttack()
                ? $"{monster.AttackName} ({monster.AttackDamageDice})"
                : "немає";

            var msg = await botClient.SendTextMessageAsync(
                userId,
                $"Монстр створений:\n" +
                $"ID: {monster.ID}\n" +
                $"Ім'я: {monster.Name}\n" +
                $"ХП: {monster.HP}/{monster.GetEffectiveMaxHP()}\n" +
                $"Клас обладунків: {monster.Armour}\n" +
                $"Кількість досвіду: {monster.XP}\n" +
                $"Ініціатива: {monster.Initiative}\n" +
                $"Атака: {attackLine}",
                cancellationToken: cancellationToken);

            OldMessages.TryAdd(masterID, [msg.MessageId]);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Залишити", "monster_keep"),
                    InlineKeyboardButton.WithCallbackData("💾 Залишити + шаблон", "monster_keep_template")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🗑 Видалити", "monster_delete")
                }
            });

            var actionMsg = await botClient.SendTextMessageAsync(
                userId,
                "Що зробити з монстром?",
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboard);

            OldMessages[userId].Add(actionMsg.MessageId);
            userState[MonsterCount] = "Delete";
        }
    }
}

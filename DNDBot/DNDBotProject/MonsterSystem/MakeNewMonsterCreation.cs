using DNDBot.BotMath;
using DNDBot.BotSystem;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.MonsterSystem
{
    class MakeNewMonsterCreation : MonsterRealisation
    {
        public static async Task MakeNewMonster(
            ITelegramBotClient botClient,
            Message message,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation,
            CancellationToken cancellationToken)
        {
            long userId = masterID;

            if (!int.TryParse(message.Text, out int count) || count <= 0)
            {
                var msg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Будь ласка, напишіть коректне цифрове значення більше за 0.",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(msg.MessageId);
                return;
            }

            if (count > 200)
            {
                var msg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Забагато монстрів за раз (максимум 200).",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(msg.MessageId);
                return;
            }

            if (newMonster == null)
            {
                var errorMsg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Помилка: базовий монстр не знайдений!",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(errorMsg.MessageId);
                return;
            }

            var baseMonster = newMonster;
            int maxHp = baseMonster.GetEffectiveMaxHP();

            for (int i = 0; i < count; i++)
            {
                var spawnedMonster = new MonsterCreator
                {
                    ID = GetNextMonsterID(),
                    Name = baseMonster.Name,
                    HP = maxHp,
                    MaxHP = maxHp,
                    Armour = baseMonster.Armour,
                    XP = baseMonster.XP,
                    Initiative = MathfDND.DiceReturn(20),
                    AttackName = baseMonster.AttackName,
                    AttackDamageDice = baseMonster.AttackDamageDice
                };

                Monsters.Add(spawnedMonster);
                MonsterCount++;
            }

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Було створено {count} монстра(-ів) за шаблоном.",
                cancellationToken: cancellationToken,
                replyMarkup: replyKeyboard);

            DeleteAllOldMessages(botClient, cancellationToken, userId);

            var replKeyboard = botBodyRealisation.ButtonsCreate(userId, false);
            var inlinKeyboard = BotKeyboardHelper.ConvertReplyKeyboardToInline(replKeyboard);
            botBodyRealisation.MiniButtonsCreate();

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                userId,
                "Невеличкі кнопочки знайшли місце у вас під клавіатурою :3",
                replyMarkup: botBodyRealisation.MiniButtonsCreate(),
                cancellationToken: cancellationToken)).MessageId);

            await botClient.SendTextMessageAsync(
                userId,
                "Меню всіх кнопочок:",
                replyMarkup: inlinKeyboard,
                cancellationToken: cancellationToken);

            isProcessingAnotherProcess[userId] = false;
            userState[MonsterCount] = "end";
            State = 0;
            newMonster = null;
        }
    }
}

using DNDBot.BotMath;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    public class WeaponRealisation : BotBodyRealisation
    {
        public static async Task Weapons(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            if (message?.From == null)
                return;

            if (isProcessingAnotherProcess[message.From.Id])
                return;

            var msgText = message.Text ?? "";
            if (Weapon.Contains(msgText))
            {
                if (WI.WeaponInfoDice[1].ContainsKey(msgText))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Ваш урон - 1", cancellationToken: cancellationToken);
                }
                if (WI.WeaponInfoDice[4].ContainsKey(msgText))
                {
                    await WeaponInfoReturn(botClient, message, 4, cancellationToken);
                }
                if (WI.WeaponInfoDice[6].ContainsKey(msgText))
                {
                    await WeaponInfoReturn(botClient, message, 6, cancellationToken);
                }
                if (WI.WeaponInfoDice[8].ContainsKey(msgText))
                {
                    await WeaponInfoReturn(botClient, message, 8, cancellationToken);
                }
                if (WI.WeaponInfoDice[10].ContainsKey(msgText))
                {
                    await WeaponInfoReturn(botClient, message, 10, cancellationToken);
                }
                if (WI.WeaponInfoDice[12].ContainsKey(msgText))
                {
                    await WeaponInfoReturn(botClient, message, 12, cancellationToken);
                }
            }
        }

        private static async Task WeaponInfoReturn(ITelegramBotClient botClient, Message message, int dice, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userID = message.From.Id;
            int DiceRoll = 0;

            switch (dice)
            {
                case 4:
                    DiceRoll = MathfDND.DiceReturn(4);
                    break;
                case 6:
                    DiceRoll = MathfDND.DiceReturn(6);
                    break;
                case 8:
                    DiceRoll = MathfDND.DiceReturn(8);
                    break;
                case 10:
                    DiceRoll = MathfDND.DiceReturn(10);
                    break;
                case 12:
                    DiceRoll = MathfDND.DiceReturn(12);
                    break;
            }

            int strenght = UserData.DataBaseValueReader<int>(userID, "Strenght");
            int agility = UserData.DataBaseValueReader<int>(userID, "Agility");

            int power = strenght > agility ? strenght : agility;
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Модифікатор - {power}\nНа кубіку - {DiceRoll}\nТотальний урон - {DiceRoll + power}", cancellationToken: cancellationToken);
        }


    }
}

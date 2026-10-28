using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace DNDBot.MonsterSystem
{
    class ExpirienceMonsterCreation : MonsterRealisation
    {

        public static async Task ExpirienceVoid(ITelegramBotClient botClient, Message message, BotBodyRealisation botBodyRealisation, CancellationToken cancellationToken)
        {
            if (int.TryParse(message.Text, out int xp))
            {
                newMonster.XP = xp;
                var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "Клас обладунків:", cancellationToken: cancellationToken);
                OldMessages[masterID].Add(msg.MessageId);
                userState[MonsterCount] = "Armor";
            }
            else
            {
                var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, напишіть цифрове значення для параметру.", cancellationToken: cancellationToken);
                OldMessages[masterID].Add(msg.MessageId);
            }
        }
    }
}

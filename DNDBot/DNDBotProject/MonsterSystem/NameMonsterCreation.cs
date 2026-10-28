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
    class NameMonsterCreation : MonsterRealisation
    {
        public static async Task NameVoid(ITelegramBotClient botClient, Message message, BotBodyRealisation botBodyRealisation, CancellationToken cancellationToken)
        {
             
            newMonster.Name = message.Text;

            if (State == 2)
            {
                var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "ХП монстра:", cancellationToken: cancellationToken);
                OldMessages[masterID].Add(msg.MessageId);
                State = 3;
            }
            userState[MonsterCount] = "health";
        }
    }
}

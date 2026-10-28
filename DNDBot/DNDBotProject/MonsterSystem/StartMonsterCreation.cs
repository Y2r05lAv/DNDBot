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
    class StartMonsterCreation : MonsterRealisation
    {
        public static async Task StartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            isProcessingAnotherProcess[userId] = true;
            newMonster = new MonsterCreator()
            {
                ID = GetNextMonsterID(),
                AttackName = "",
                AttackDamageDice = ""
            };

            var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "Ім'я  монстра:", cancellationToken: cancellationToken);

            if(OldMessages.ContainsKey(masterID))
                OldMessages[masterID].Add(msg.MessageId);
            else
            {
                OldMessages.Add(masterID, []);
                OldMessages[masterID].Add(msg.MessageId);

            }

            State = 2;
            userState[MonsterCount] = "name";
        }
    }
}

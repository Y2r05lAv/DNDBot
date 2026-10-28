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
    class HealthMonsterCreation : MonsterRealisation
    {
        public static async Task HealthVoid(ITelegramBotClient botClient, Message message, BotBodyRealisation botBodyRealisation, CancellationToken cancellationToken)
        {
             
            if (int.TryParse(message.Text, out int health))
            {
                newMonster.HP = health;
                newMonster.MaxHP = health;
                var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть кількість досвіду для монстра:", cancellationToken: cancellationToken);
                OldMessages[masterID].Add(msg.MessageId);
                userState[MonsterCount] = "Expirience";
            }
            else
            {
                var msg = await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, напишіть цифрове значення для параметру.", cancellationToken: cancellationToken);
                OldMessages[masterID].Add(msg.MessageId);
            }
        }
    }
}

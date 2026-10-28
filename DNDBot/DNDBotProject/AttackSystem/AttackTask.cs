using DNDBot.BotMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace DNDBot.AttackSystem
{
    class AttackTask : BotBodyRealisation
    {
        private readonly ILogger<AttackTask> _logger;
        private readonly DamageTask _damageTask;

        public AttackTask(ILogger<AttackTask> logger, DamageTask damageTask)
        {
            _logger = logger;
            _damageTask = damageTask;
        }



        public async Task Attack(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message, long attackID)
        {
            _logger.LogInformation($"Starting attack process. Attack ID: {attackID}");

            if (message == null || message.Chat == null)
                return;

             
            if (!int.TryParse(message.Text, out int damage))
            {
                _logger.LogWarning("Invalid damage value.");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Некоректне значення урону!", cancellationToken: cancellationToken);
                return;
            }

             
            if (attackID > 999)
            {
                _logger.LogInformation($"Attacking special entity with ID: {attackID}");

                damagedCreature = attackID;
                PlayerHit = -1;
                userStateAtack[0] = "ToDamage";

                await _damageTask.Damage(botClient, cancellationToken, message, damage);
                return;
            }


             
            if (Monsters == null || Monsters.Count == 0)
            {
                _logger.LogWarning("No monsters found in the dictionary.");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Сутності відсутні!", cancellationToken: cancellationToken);
                return;
            }

            var monster = Monsters.FirstOrDefault(m => m != null && m.ID == attackID);

             
            if (monster == null)
            {
                _logger.LogWarning($"Monster with ID {attackID} not found.");
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Із таким айді {attackID} не знайдено!", cancellationToken: cancellationToken);
                return;
            }

            _logger.LogInformation($"Target monster found: {monster.Name} (ID: {monster.ID}). Checking attack...");

             
             
             
             

             
             
             
             

             
             
             
             

            _logger.LogInformation($"Successful hit!");  

             
            damagedCreature = attackID;
            userStateAtack[0] = "ToDamage";

             
             
             
             

            await _damageTask.Damage(botClient, cancellationToken, message, damage);
        }






        public void AreHit(Message msg)
        {
            if (msg?.From == null)
                return;

            long userID = msg.From.Id;
            if (FightCharacter.ContainsKey(userID) ? UserData.CheckifExist(userID, FightCharacter[userID]) : UserData.CheckifExist(userID, UserData.GetActiveCharacterId(userID)))
            {
               int characterId = 0;
               if (!FightCharacter.TryGetValue(userID, out characterId) || characterId <= 0)
               {
                                        
                   characterId = UserData.GetActiveCharacterId(userID);
                   FightCharacter[userID] = characterId;
               }
                
                int Power = UserData.DataBaseValueReader<int>(userID, characterId, "Strenght");  
                int Speed = UserData.DataBaseValueReader<int>(userID, characterId, "Agility");  
                int ToUseInt = Power > Speed ? Power : Speed;
                int calOp = MathfDND.DiceReturn(20);
                PlayerHit = calOp + ToUseInt;
            }
        }
    }
}

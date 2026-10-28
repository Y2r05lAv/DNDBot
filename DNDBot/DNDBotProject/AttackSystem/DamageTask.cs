using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace DNDBot.AttackSystem
{
    class DamageTask : BotBodyRealisation
    {
        private readonly ILogger<DamageTask> _logger;
        private readonly BotBodyRealisation _botBodyRealisation;

        public DamageTask(ILogger<DamageTask> logger, BotBodyRealisation botBodyRealisation)
        {
            _logger = logger;
            _botBodyRealisation = botBodyRealisation;
        }



        public async Task Damage(ITelegramBotClient botClient, CancellationToken cancellationToken, Message msg, int damage)
        {
            if (damage == 0)
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Неправильний тип урону: {damage}.", cancellationToken: cancellationToken);
                _logger.LogInformation($"Not right variable format: {damage}");
                return;

            }

            _logger.LogInformation($"Got damage variable. The search has begun: Monster or User");

            damage = Math.Abs(damage);

            await MonsterCase(botClient, msg, damage, cancellationToken);
            await UserCase(botClient, msg, damage, cancellationToken);

        }

        private async Task UserCase(ITelegramBotClient botClient, Message msg, int damage, CancellationToken cancellationToken)
        {
            if (damagedCreatureCharacterId <= 0 || !UserData.CheckifExist(damagedCreature, damagedCreatureCharacterId))
            {
                if(!Monsters.Any(m => m.ID == damagedCreature))
                    _logger.LogInformation($"User and monster with {damagedCreature} id was NOT Found in UserData.");

                return;
            }

            string name = UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name");
            var damageResult = UserData.ApplyDamageWithTemporaryHp(damagedCreature, damagedCreatureCharacterId, damage);

            string healthDisplay = UserData.BuildHealthDisplay(
                damageResult.CurrentHp,
                damageResult.MaxHp,
                damageResult.TempHp);

            string absorbedText = damageResult.DamageToTempHp > 0
                ? $" ({damageResult.DamageToTempHp} поглинуто тимчасовими хітами)"
                : string.Empty;

            userStateAtack[0] = "";
            PlayerHit = -1;
            await botClient.SendTextMessageAsync(
                msg.Chat.Id,
                $"Було нанесено гравцю {name} на {damage}{absorbedText}, у гравця зараз {healthDisplay} здоров'я",
                cancellationToken: cancellationToken);

            await ReturnToCombatMenuIfActive(botClient, msg.Chat.Id, cancellationToken);
        }

        private async Task MonsterCase(ITelegramBotClient botClient, Message msg, int damage, CancellationToken cancellationToken)
        {
            var monster = Monsters.FirstOrDefault(m => m != null && m.ID == damagedCreature);

            if (monster == null)
            {
                _logger.LogWarning($"Monster with ID {damagedCreature} not found. Trying to find player.");
                return;
            }

            _logger.LogInformation($"Monster with ID {damagedCreature} exists. Current HP: {monster.HP}");

            if (monster.MaxHP <= 0)
                monster.MaxHP = monster.HP;

            monster.HP -= damage;  

            if (monster.HP > 0)  
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id,
                    $"Було атаковано {monster.Name} (ID: {monster.ID}) на {damage} урону. Залишилось {monster.HP} HP.",
                    cancellationToken: cancellationToken);

                await ReturnToCombatMenuIfActive(botClient, msg.Chat.Id, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id,
                    $"Було вбито *{monster.Name}* (ID: {monster.ID})",
                    
                    cancellationToken: cancellationToken);

                _botBodyRealisation.RemoveMonsterFromAllCollections(damagedCreature);

                await ReturnToCombatMenuIfActive(botClient, msg.Chat.Id, cancellationToken);
            }

            userStateAtack[0] = "";
            PlayerHit = -1;
        }

        private static async Task ReturnToCombatMenuIfActive(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (!global::DNDBot.ComandsRealisation.IsCombatSessionActive(chatId))
                return;

            await botClient.SendTextMessageAsync(
                chatId,
                "Повернення до бойової панелі:",
                replyMarkup: global::DNDBot.ComandsRealisation.GetCombatPanelMarkup(chatId),
                cancellationToken: cancellationToken);
        }

    }
}

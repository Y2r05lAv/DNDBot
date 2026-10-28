using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace DNDBot.AttackSystem
{
    class HealCommands : BotBodyRealisation
    {
        private readonly ILogger<HealCommands> _logger;

        public HealCommands(ILogger<HealCommands> logger)
        {
            _logger = logger;
        }

        public async Task ToHealPlayer(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message)
        {
            if (message == null)
                return;

            if (int.TryParse(message.Text, out int HealCount))
            {
                _logger.LogInformation("Successful string parse, Heal count == {heal}", HealCount);

                if (damagedCreatureCharacterId > 0 && UserData.CheckifExist(damagedCreature, damagedCreatureCharacterId))
                {
                    int hp = UserData.DataBaseValueReader<int>(damagedCreature, damagedCreatureCharacterId, "Health");
                    int maxHP = UserData.DataBaseValueReader<int>(damagedCreature, damagedCreatureCharacterId, "MaxHp");
                    int tempHP = UserData.DataBaseValueReader<int>(damagedCreature, damagedCreatureCharacterId, "TempHP");
                    string name = UserData.DataBaseValueReader<string>(damagedCreature, damagedCreatureCharacterId, "Name");

                    _logger.LogInformation("Past hp {name} HP == {heal}", name, hp);

                    hp = hp + HealCount >= maxHP ? maxHP : hp + HealCount;

                    _logger.LogInformation("Current {name} HP == {heal}", name, hp);

                    UserData.UpdateDataBaseValue("Health", damagedCreature, damagedCreatureCharacterId, hp);

                    string healthDisplay = UserData.BuildHealthDisplay(hp, maxHP, tempHP);
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        $"Було вилікувано {name} на {HealCount}, у цілі зараз {healthDisplay}",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var monster = Monsters.FirstOrDefault(m => m != null && m.ID == damagedCreature);
                    if (monster == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Немає даних про ціль.", cancellationToken: cancellationToken);
                        return;
                    }

                    int maxHp = monster.GetEffectiveMaxHP();
                    int oldHp = monster.HP;
                    monster.HP = Math.Min(maxHp, monster.HP + HealCount);

                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        $"Було вилікувано {monster.Name} на {monster.HP - oldHp}, зараз {monster.HP}/{maxHp} HP.",
                        cancellationToken: cancellationToken);
                }

                await ReturnToCombatMenuIfActive(botClient, message.Chat.Id, cancellationToken);

                _logger.LogInformation("Task completed.");

                HealState = "";
            }
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

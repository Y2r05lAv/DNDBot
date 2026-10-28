using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using DNDBot.BotMath;
using Microsoft.Extensions.Logging;

namespace DNDBot.AttackSystem
{
    class FightTask : BotBodyRealisation
    {
        public sealed class CombatTurnParticipant
        {
            public long Id { get; init; }
            public int CharacterId { get; init; }
            public string Name { get; init; } = "";
            public int Initiative { get; init; }
        }

        private readonly ILogger<FightTask> _logger;
        public FightTask(ILogger<FightTask> logger)
        {
            _logger = logger;
        }



        public async Task Fight(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message, string state)
        {
            string playerInitiative = "";
            bool sortByInitiative = state == "Бій";
            bool hasPendingPlayerRolls = InitiativeUsers.Count > 0;
            bool shouldRebuildFightSnapshot = Initiative.Count == 0;

            if (shouldRebuildFightSnapshot)
            {
                Initiative.Clear();
                BuildFightSnapshot(state);
                SyncFightCharactersFromSnapshot();
            }
            else
            {
                PruneInvalidInitiativeEntries();
                 
                BuildFightSnapshot(state);
                SyncFightCharactersFromSnapshot();
            }

             
            if (hasPendingPlayerRolls)
                InitiativeUsers.Clear();

            DebugInitiativeCollections();

            if (Initiative.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Немає учасників бою. Спочатку гравці мають кинути ініціативу, а майстер запустити бій.",
                    cancellationToken: cancellationToken);
                return;
            }

            PrintFightIndex(sortByInitiative, ref playerInitiative);

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Порядок бою:\n{playerInitiative}",
                cancellationToken: cancellationToken
            );


            playerInitiative = "";
        }

        private void DebugInitiativeCollections()  
        {
            _logger.LogInformation("Monster state:");
            foreach (var monster in Monsters)
            {
                _logger.LogInformation($"{monster.ID}: {monster.Name}, HP: {monster.HP}");
            }

            _logger.LogInformation("Initiative state:");
            foreach (var initiative in Initiative)
            {
                _logger.LogInformation($"{initiative.Key}: Name: {initiative.Value.First().Key}, Initiative: {initiative.Value.First().Value}");
            }


        }


        private void InitiativUserseAdd()
        {
            foreach (var userInitiative in InitiativeUsers)
            {
                 
                if (!Initiative.ContainsKey(userInitiative.Key))
                {
                    long uid = userInitiative.Key.userId;
                    int cid = userInitiative.Key.characterId;

                    string name = (cid > 0 && UserData.CheckifExist(uid, cid))
                        ? UserData.DataBaseValueReader<string>(uid, cid, "Name")
                        : UserData.DataBaseValueReader<string>(uid, "Name");  

                    Initiative.Add(userInitiative.Key, new Dictionary<string, int>
            {
                { name, userInitiative.Value.First().Value }
            });
                }
            }
        }

        private void BuildFightSnapshot(string state)
        {
            switch (state)
            {
                case "Заскочені гравці":
                    InitiativeMonstersAdd();
                    InitiativUserseAdd();
                    break;
                case "Бій":
                case "Заскочені монстри":
                default:
                    InitiativUserseAdd();
                    InitiativeMonstersAdd();
                    break;
            }
        }

        private void PruneInvalidInitiativeEntries()
        {
            var invalidKeys = Initiative.Keys
                .Where(key =>
                {
                    if (key.characterId > 0)
                        return !UserData.CheckifExist(key.userId, key.characterId);

                    return !Monsters.Any(monster => monster != null && monster.ID == key.userId);
                })
                .ToList();

            foreach (var key in invalidKeys)
                Initiative.Remove(key);
        }

        private void SyncFightCharactersFromSnapshot()
        {
            FightCharacter.Clear();

            foreach (var key in Initiative.Keys.Where(key => key.characterId > 0))
                FightCharacter[key.userId] = key.characterId;
        }


        private void InitiativeMonstersAdd()
        {
            foreach (var monster in Monsters.Where(m => m != null))
            {
                var key = (monster.ID, 0);  

                if (!Initiative.ContainsKey(key))
                {
                    Initiative.Add(key, new Dictionary<string, int>
            {
                { monster.Name, monster.Initiative }
            });
                }
            }
        }

        public List<CombatTurnParticipant> GetOrderedParticipants(bool sortByInitiative = true)
        {
            var result = new List<CombatTurnParticipant>();

            IEnumerable<KeyValuePair<(long userId, int characterId), Dictionary<string, int>>> source =
                sortByInitiative
                    ? Initiative.OrderByDescending(entry => entry.Value.First().Value)
                    : Initiative;

            foreach (var entry in source)
            {
                var firstInnerEntry = entry.Value.First();

                long id = entry.Key.userId;
                int characterId = entry.Key.characterId;
                int initiative = firstInnerEntry.Value;

                string name;
                if (characterId > 0 && UserData.CheckifExist(id, characterId))
                    name = UserData.DataBaseValueReader<string>(id, characterId, "Name");
                else
                    name = firstInnerEntry.Key;

                result.Add(new CombatTurnParticipant
                {
                    Id = id,
                    CharacterId = characterId,
                    Name = name,
                    Initiative = initiative
                });
            }

            return result;
        }




        ///




        private void PrintFightIndex(bool FightContext, ref string playerInitiative)
        {
            _logger.LogInformation("Starting to process the initiative order.");
            var orderedParticipants = GetOrderedParticipants(FightContext);
            foreach (var entry in orderedParticipants)
            {
                long id = entry.Id;
                int characterId = entry.CharacterId;
                string name = entry.Name;

                 
                string escapedKey = id < 0 ? "\u2010" + (-id) : id.ToString();

                 
                 
                string charMark = characterId > 0 ? $" (C{characterId})" : "";

                playerInitiative += $"Ім'я: {name} {charMark}, Ініціатива: {entry.Initiative}, Айді: {escapedKey}\n";
            }

            _logger.LogInformation("Finished processing the initiative order.");
        }


    }
}

using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    public class LevelSystem : BotBodyRealisation
    {
        public static Dictionary<long, long> choosenUser = new();
        public static Dictionary<long, int> choosenCharacter = new();
        public static bool AddToAllUsers = false;

        private readonly int perLevelBaseIncrease = 7;

        private readonly ILogger<LevelSystem> _logger;
        public static List<int> ManaConsumption = new List<int> { 0 };

        public LevelSystem(ILogger<LevelSystem> logger)
        {
            _logger = logger;

            for (int i = 1; i <= 250; i++)
                ManaConsumption.Add((int)(100 * Math.Pow(1.15, i)));
        }

        private static bool TryGetNextLevelXpRequirement(int currentLevel, out int nextLevelXp)
        {
            return XpConsuption.TryGetValue(currentLevel + 1, out nextLevelXp);
        }

        private static string BuildXpProgressDisplay(int level, int xp)
        {
            if (TryGetNextLevelXpRequirement(level, out int nextLevelXp))
                return $"{xp}/{nextLevelXp}";

            return $"{xp}/MAX";
        }

        public void AddExpirience(int expirience, long userId, int charId)
        {
            int CurrentXp = UserData.DataBaseValueReader<int>(userId, charId, "XP");
            int CurrentLvl = UserData.DataBaseValueReader<int>(userId, charId, "Level");
            int NewXp = CurrentXp + expirience;

            if (!TryGetNextLevelXpRequirement(CurrentLvl, out int nextLevelXp))
            {
                UserData.UpdateDataBaseValue("XP", userId, charId, NewXp);
                return;
            }

            if (NewXp < nextLevelXp)
            {
                UserData.UpdateDataBaseValue("XP", userId, charId, NewXp);
            }
            else
            {
                while (TryGetNextLevelXpRequirement(CurrentLvl, out int threshold) && NewXp >= threshold)
                    CurrentLvl++;

                UserData.UpdateDataBaseValue("XP", userId, charId, NewXp);
                UserData.UpdateDataBaseValue("Level", userId, charId, CurrentLvl);
            }
        }

        public void AddMana(int mana, long userId, int charId)
        {
            int currentManaXP = ManaData.DataBaseValueReader<int>(userId, charId, "MagicXP");
            int currentLevel = ManaData.DataBaseValueReader<int>(userId, charId, "MagicLevel");

            int newManaXP = currentManaXP + mana;
            int oldLevel = currentLevel;

            while (currentLevel + 1 < ManaConsumption.Count && newManaXP >= ManaConsumption[currentLevel + 1])
            {
                newManaXP -= ManaConsumption[currentLevel + 1];
                currentLevel++;
            }

            ManaData.UpdateMagicStat(userId, charId, "MagicXP", newManaXP);
            ManaData.UpdateMagicStat(userId, charId, "MagicLevel", currentLevel);

            if (currentLevel > oldLevel)
            {
                try
                {
                    int currentBaseMana = ManaData.DataBaseValueReader<int>(userId, charId, "BaseMana");
                    int currentMaxMana = ManaData.DataBaseValueReader<int>(userId, charId, "MaxMana");

                    int newBaseMana = CalculateBaseManaForNewLevel(currentBaseMana, oldLevel, currentLevel);
                    int newMaxMana = GetEffectiveMaxMana(newBaseMana, currentLevel);

                    ManaData.UpdateMagicStat(userId, charId, "BaseMana", newBaseMana);
                    ManaData.UpdateMagicStat(userId, charId, "MaxMana", newMaxMana);

                    ResourceModifiers.ApplyManaModifiersAfterRecalc(userId, charId, keepCurrentProportion: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while updating BaseMana/MaxMana for user {UserId} char {CharId}", userId, charId);
                }
            }
        }

        private int GetEffectiveMaxMana(int baseMana, int magicLevel)
        {
            double multiplier = Math.Pow(1.12, magicLevel - 1);
            return (int)Math.Round(baseMana * multiplier);
        }

        private int CalculateBaseManaForNewLevel(int currentBase, int oldLevel, int newLevel)
        {
            if (currentBase <= 0) currentBase = 100;
            int levelsGained = Math.Max(0, newLevel - oldLevel);
            return currentBase + levelsGained * perLevelBaseIncrease;
        }

        public void AddResourceToUser(string resourceType, int amount, long userId, bool allCharacters = false)
        {
            var characters = allCharacters
                ? UserData.GetUserCharacterIds(userId)
                : new List<int> { UserData.GetActiveCharacterId(userId) };

            foreach (var charId in characters)
            {
                switch (resourceType)
                {
                    case "XP":
                        AddExpirience(amount, userId, charId);
                        break;
                    case "Mana":
                        AddMana(amount, userId, charId);
                        break;
                }
            }
        }


        public async Task SendResourceMessage(ITelegramBotClient botClient, long userId, string resourceType, int amount, CancellationToken cancellationToken)
        {
            var characters = UserData.GetUserCharacterIds(userId);
            string text = resourceType == "Mana" ? "магічного досвіду" : "досвіду";
            string msg = $"Додано {amount} {text} для ваших персонажів:\n";

            foreach (var charId in characters)
            {
                int val = resourceType == "XP"
                    ? UserData.DataBaseValueReader<int>(userId, charId, "XP")
                    : ManaData.DataBaseValueReader<int>(userId, charId, "MagicXP");

                int level = resourceType == "XP"
                    ? UserData.DataBaseValueReader<int>(userId, charId, "Level")
                    : ManaData.DataBaseValueReader<int>(userId, charId, "MagicLevel");

                string progress = resourceType == "XP"
                    ? BuildXpProgressDisplay(level, val)
                    : $"{val}/{(ManaConsumption.Count > level + 1 ? ManaConsumption[level + 1] : GetEffectiveMaxMana(ManaData.DataBaseValueReader<int>(userId, charId, "BaseMana"), level))}";

                string charName = "";
                try
                {
                    charName = UserData.GetCharacterName(userId, charId);
                }
                catch
                {
                    charName = "";
                }

                if (!string.IsNullOrWhiteSpace(charName))
                    msg += $"{charName} (C{charId}): {progress}, рівень {level}\n";
                else
                    msg += $"Персонаж {charId}: {progress}, рівень {level}\n";
            }

            await botClient.SendTextMessageAsync(userId, msg, cancellationToken: cancellationToken);
        }

        public async Task AddExpirienceToAllUsers(ITelegramBotClient botClient, CancellationToken cancellationToken, int expirience)
        {
            AddToAllUsers = false;

            var userIds = UserData.GetUsersFromDatabase()
                .Select(u => u.Id)
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                try
                {
                    var characterIds = UserData.GetUserCharacterIds(userId);
                    foreach (var charId in characterIds)
                        AddExpirience(expirience, userId, charId);

                    await SendResourceMessage(botClient, userId, "XP", expirience, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while adding experience to user {UserId}", userId);
                }
            }
        }

        public async Task AddManaToAllUsers(ITelegramBotClient botClient, CancellationToken cancellationToken, int mana)
        {
            AddToAllUsers = false;

            var userIds = UserData.GetUsersFromDatabase()
                .Select(u => u.Id)
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                try
                {
                    var characterIds = UserData.GetUserCharacterIds(userId);
                    foreach (var charId in characterIds)
                        AddMana(mana, userId, charId);

                    await SendResourceMessage(botClient, userId, "Mana", mana, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while adding mana to user {UserId}", userId);
                }
            }
        }
        public async Task AddExpirienceToUser(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;

            if (message == null)
                return;

            if (userStateXP.TryGetValue(userId, out string value) && !string.IsNullOrWhiteSpace(value))
            {
                switch (value)
                {
                    case "start":
                        await StartVoid(message, false);
                        break;
                    case "paste":
                        await PasteVoid(botClient, message, cancellationToken);
                        break;
                }
            }
            else if (userStateMagicXP.TryGetValue(userId, out string value1) && !string.IsNullOrWhiteSpace(value1))
            {
                switch (value1)
                {
                    case "start":
                        await StartVoid(message, true);
                        break;
                    case "paste":
                        await PasteVoid(botClient, message, cancellationToken, true);
                        break;
                }
            }
        }

        public Task StartVoid(Message message, bool magicXp = false)
        {
            if (message?.From == null)
                return Task.CompletedTask;

            long userID = message.From.Id;
            _logger.LogInformation("Starting AddExpirience cycle for user {UserID}", userID);

            if (!StateBotMSG.ContainsKey(userID))
                StateBotMSG[userID] = 0;
            StateBotMSG[userID] = 1;

            if (!OldMessages.ContainsKey(userID))
                OldMessages.Add(userID, new List<int>());

            if(message != null)
                OldMessages[userID].Add(message.MessageId);
            
            if (!magicXp)
            {
                userStateXP[userID] = "paste";
                userStateMagicXP[userID] = "";
            }
            else
            {
                userStateMagicXP[userID] = "paste";
                userStateXP[userID] = "";
            }
             
            return Task.CompletedTask;
        }

        private async Task PasteVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, bool magicXp = false)
        {
            if (message?.From == null)
                return;

            long userID = message.From.Id;
            int resultInt = 0;

            if (!StateBotMSG.TryGetValue(userID, out long currentState))
                currentState = 0;
            string inputText = (message.Text ?? string.Empty).Trim();
            int.TryParse(inputText, out resultInt);


            if (!OldMessages.ContainsKey(userID))
                OldMessages[userID] = new List<int>();

            DeleteAllOldMessages(botClient, cancellationToken, userID);

            if (!AddToAllUsers)
            {
                long targetUserId = userID;
                if (userID == masterID && choosenUser.TryGetValue(masterID, out long selectedUserId))
                    targetUserId = selectedUserId;

                int targetCharId = UserData.GetActiveCharacterId(targetUserId);
                if (userID == masterID && choosenCharacter.TryGetValue(masterID, out int selectedCharacterId) && selectedCharacterId > 0)
                    targetCharId = selectedCharacterId;

                if (!magicXp)
                    AddExpirience(resultInt, targetUserId, targetCharId);
                else
                    AddMana(resultInt, targetUserId, targetCharId);

                if (!magicXp)
                    await botClient.SendTextMessageAsync(targetUserId, "Дані було додано!\n" + UserData.DataBaseReader(targetUserId, targetCharId), cancellationToken: cancellationToken);
                else
                    await botClient.SendTextMessageAsync(targetUserId, "Дані було додано!\n" + BuildPlayerView(ManaData.GetMagicStats(targetUserId, targetCharId)), cancellationToken: cancellationToken);

                if (userID == masterID)
                    await botClient.SendTextMessageAsync(masterID, "Дані було додано до:\n" + UserData.DataBaseReader(targetUserId, targetCharId), cancellationToken: cancellationToken);

            }
            else
            {
                AddToAllUsers = false;
                if (!magicXp) await AddExpirienceToAllUsers(botClient, cancellationToken, resultInt); else await AddManaToAllUsers(botClient, cancellationToken, resultInt);
            }

            userStateXP[userID] = "";
            userStateMagicXP[userID] = "";

            if (userID == masterID)
            {
                choosenUser.Remove(masterID);
                choosenCharacter.Remove(masterID);
            }

            StateBotMSG[userID] = 1;

            if (!StateBotMSG.ContainsKey(userID))
                StateBotMSG[userID] = 0;
            StateBotMSG[userID] = StateBotMSG[userID] - 1;
        }

        public string BuildPlayerView(ManaDataClass s)
        {
            string baseManaSuffix = "";
            string maxManaSuffix = "";
            string currentManaSuffix = "";

            try
            {
                var mods = ResourceModifiers.Get(s.UserId, s.CharacterId);
                if (mods != null)
                {
                    if (mods.BaseManaPercentMod != 0) baseManaSuffix = $" ({(mods.BaseManaPercentMod > 0 ? "+" : "")}{mods.BaseManaPercentMod}%)";
                    if (mods.MaxManaPercentMod != 0) maxManaSuffix = $" ({(mods.MaxManaPercentMod > 0 ? "+" : "")}{mods.MaxManaPercentMod}%)";
                    if (mods.CurrentManaPercentMod != 0) currentManaSuffix = $" ({(mods.CurrentManaPercentMod > 0 ? "+" : "")}{mods.CurrentManaPercentMod}%)";
                }
            }
            catch
            {
            }

            return
            $@"🔮 Магічні характеристики

            Магічний рівень: {s.MagicLevel}
            Магічний досвід: {s.MagicXP}/{ManaConsumption[s.MagicLevel + 1]}

            Мана: {s.CurrentMana}{currentManaSuffix} / {s.MaxMana}{maxManaSuffix}
            Базова мана: {s.BaseMana}{baseManaSuffix}

            Магічний обладунок: {s.MagicArmour}

            Стабільність: {s.Stability}%
            Виснаження: {s.Exhaustion}%

            Зона мани:
            • Радіус: {s.ManaZoneRadius} м
            • Контроль: {s.ManaZoneControl}%

            Регенерація:
            • За хід: +{Math.Round(s.MaxMana / 100.0 + s.RegenTurn, 2)}
            • Після бою: +{Math.Round(s.MaxMana / 10.0 + s.RegenAfterFight, 2)}";
        }

    }
}

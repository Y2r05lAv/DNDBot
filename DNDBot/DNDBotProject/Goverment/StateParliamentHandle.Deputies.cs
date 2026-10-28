using DNDBot.DataBase;
using DNDBot.BotSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private enum DeputyCreationMode
        {
            Manual,
            Fast
        }

        private static readonly Dictionary<long, DeputyCreationMode> DeputyCreationModeByUser = new();
        private static readonly Dictionary<long, int> DeputyCreationStepByUser = new();
        private static readonly Dictionary<long, DeputyRecord> DeputyCreationBufferByUser = new();
        private static readonly Dictionary<long, int> DeputyCreationCountryByUser = new();
        private static readonly Dictionary<long, int> DeputyFastCountByUser = new();
        private static readonly HashSet<long> AwaitingDeputyFastCount = new();
        private static readonly Dictionary<long, (long ChatId, int MessageId)> LastDeputiesMenuByUser = new();
        private const int LoyalDeputiesDefaultCount = 10;
        private const int LoyalDeputiesMinCount = 1;
        private const int LoyalDeputiesMaxCount = 200;

        private static readonly string[] DeputyFirstNames = new[]
        {
            "Ivan", "Petro", "Andrii", "Oleh", "Dmytro", "Serhii", "Mykola", "Taras",
            "Viktor", "Yaroslav", "Bohdan", "Maksym", "Roman", "Artem", "Pavlo"
        };

        private static readonly string[] DeputyLastNames = new[]
        {
            "Shevchenko", "Ivanenko", "Petrenko", "Koval", "Bondar", "Tkachenko",
            "Moroz", "Kravchenko", "Kovalchuk", "Savchenko", "Melnyk", "Boyko"
        };

        private static readonly string[] LoyalDeputiesCommandPrefixes = new[]
        {
            "створити покірних і лояльних депутатів",
            "створити покірних і лояхльних депутатів",
            "створити покірних депутатів",
            "створити лояльних депутатів",
            "/loyal_deputies",
            "/obedient_deputies"
        };

        private async Task HandleDeputySelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, int deputeeId, CancellationToken cancellationToken)
        {
            var deputy = DeputiesData.GetDeputyById(deputeeId);
            if (deputy == null)
            {
                await botClient.SendTextMessageAsync(callbackQuery.From.Id, "Депутат не знайдений.", cancellationToken: cancellationToken);
                return;
            }

            string text = BuildDeputyInfoText(deputy);

            await botClient.SendTextMessageAsync(callbackQuery.From.Id, text, cancellationToken: cancellationToken);
        }

        private async Task HandleCreateLoyalDeputiesTextCommand(ITelegramBotClient botClient, long userId, int? requestedCount, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            int count = requestedCount ?? LoyalDeputiesDefaultCount;
            if (count < LoyalDeputiesMinCount || count > LoyalDeputiesMaxCount)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    $"Невірна кількість. Вкажіть число від {LoyalDeputiesMinCount} до {LoyalDeputiesMaxCount}.\n" +
                    "Формат: Створити покірних і лояльних депутатів 25",
                    cancellationToken: cancellationToken);
                return;
            }

            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            var targetParty = parties
                .Where(p => p.GovernmentAligned)
                .OrderByDescending(p => p.Discipline)
                .ThenByDescending(p => p.Influence)
                .FirstOrDefault()
                ?? parties
                    .OrderByDescending(p => p.Discipline)
                    .ThenByDescending(p => p.Influence)
                    .FirstOrDefault();

            int? partyId = targetParty?.PartyId;
            string partyLabel = targetParty?.Name ?? "Без партії";

            int created = 0;
            int partyLinkErrors = 0;
            var createdExamples = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var deputy = new DeputyRecord
                {
                    CountryId = countryId,
                    PartyId = partyId,
                    Name = GenerateDeputyName(),
                    Loyalty = Rng.Next(86, 101),
                    Corruption = Rng.Next(0, 19),
                    Influence = Rng.Next(45, 86),
                    AttendanceChance = Rng.Next(84, 101),
                    IsAlive = true
                };

                int? newId = DeputiesData.AddDeputyAndReturnId(deputy);
                if (!newId.HasValue)
                    continue;

                created++;
                if (createdExamples.Count < 8)
                    createdExamples.Add($"{deputy.Name} #{newId.Value}");

                if (partyId.HasValue && partyId.Value > 0)
                {
                    try
                    {
                        PartyMembersData.AddPartyMember(new PartyMemberRecord
                        {
                            PartyId = partyId.Value,
                            DeputyId = newId.Value
                        });
                    }
                    catch (Exception ex)
                    {
                        partyLinkErrors++;
                        _logger.LogError(ex,
                            "Failed to assign loyal deputy to party. DeputyId={DeputyId} PartyId={PartyId} UserId={UserId}",
                            newId.Value, partyId.Value, userId);
                    }
                }
            }

            if (created > 0)
                DisableAutoParliamentStructure(countryId);

            await RefreshDeputiesMenuIfExists(botClient, userId, cancellationToken);

            string text =
                $"✅ Створено покірних депутатів: {created}/{count}.\n" +
                $"Фракція: {partyLabel}\n" +
                $"Профіль: лояльність 86-100%, корупція 0-18%, відвідуваність 84-100%.";

            if (createdExamples.Count > 0)
                text += $"\nПриклади: {string.Join(", ", createdExamples)}";

            if (partyLinkErrors > 0)
                text += $"\n⚠ Не вдалося прив'язати до фракції: {partyLinkErrors}.";

            await botClient.SendTextMessageAsync(userId, text, cancellationToken: cancellationToken);
        }

        private async Task EditMessageInfoForDeputies(ITelegramBotClient botClient, long chatId, int msgId, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    msgId,
                    text: error,
                    replyMarkup: StateKeyboardBuilder.DeputyCommandsKeyboard(false),
                    cancellationToken: cancellationToken
                );
                return;
            }

            List<DeputyRecord> deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId)
                .ToList();
            bool hasDeputies = deputies.Count != 0;

            double avgLoyalty = hasDeputies ? deputies.Average(d => d.Loyalty) : 0;
            double avgCorruption = hasDeputies ? deputies.Average(d => d.Corruption) : 0;

            string text =
                $"👥 Депутати\n\n" +
                $"Усього депутатів: {deputies.Count}\n" +
                $"Середня підтримка влади: {avgLoyalty:0.##}%\n" +
                $"Середній процент корупції: {avgCorruption:0.##}%\n";

            LastDeputiesMenuByUser[userId] = (chatId, msgId);

            try
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    msgId,
                    text: text,
                    replyMarkup: StateKeyboardBuilder.DeputyCommandsKeyboard(hasDeputies),
                    cancellationToken: cancellationToken
                );
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.Message != null && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _logger.LogError(ex, "EditMessageInfoForDeputies failed. ChatId={ChatId} MsgId={MsgId} UserId={UserId} ErrorCode={ErrorCode} Error={Error}",
                    chatId, msgId, userId, ex.ErrorCode, ex.Message);
                throw;
            }
        }

        private async Task StartDeputyCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            ClearDeputyCreationState(userId);
            DeputyCreationModeByUser[userId] = DeputyCreationMode.Manual;
            DeputyCreationCountryByUser[userId] = countryId;
            DeputyCreationStepByUser[userId] = 0;
            DeputyCreationBufferByUser[userId] = new DeputyRecord
            {
                CountryId = countryId,
                IsAlive = true
            };

            var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть ім'я депутата:", cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task StartDeputyFastCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            ClearDeputyCreationState(userId);
            DeputyCreationModeByUser[userId] = DeputyCreationMode.Fast;
            DeputyCreationCountryByUser[userId] = countryId;
            AwaitingDeputyFastCount.Add(userId);

            var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть кількість депутатів для швидкого створення (1-200):", cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task HandleDeputyFastCountInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            string input = (message.Text ?? string.Empty).Trim();
            TrackOldMessage(userId, message.MessageId);

            if (!int.TryParse(input, out int count) || count < 1 || count > 200)
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 1 до 200:", cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            AwaitingDeputyFastCount.Remove(userId);
            DeputyFastCountByUser[userId] = count;

            if (!DeputyCreationCountryByUser.TryGetValue(userId, out int countryId))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, "Не знайдено країни для створення.", cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            await SendDeputyPartySelection(botClient, userId, countryId, isFast: true, cancellationToken: cancellationToken);
        }

        private async Task HandleDeputyManualInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!DeputyCreationStepByUser.TryGetValue(userId, out int step) || !DeputyCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            TrackOldMessage(userId, message.MessageId);

            string input = (message.Text ?? string.Empty).Trim();
            if (step == 0)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    var errMsg = await botClient.SendTextMessageAsync(userId, "Ім'я не може бути порожнім. Спробуйте ще раз:", cancellationToken: cancellationToken);
                    TrackOldMessage(userId, errMsg.MessageId);
                    return;
                }

                buffer.Name = input;
                DeputyCreationStepByUser[userId] = 1;
                var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть рівень лояльності (0-100):", cancellationToken: cancellationToken);
                TrackOldMessage(userId, msg.MessageId);
                return;
            }

            if (!TryParsePercent(input, out int value))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 0 до 100:", cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            switch (step)
            {
                case 1:
                    buffer.Loyalty = value;
                    DeputyCreationStepByUser[userId] = 2;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть рівень корупції (0-100):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 2:
                    buffer.Corruption = value;
                    DeputyCreationStepByUser[userId] = 3;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть рівень впливу (0-100):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 3:
                    buffer.Influence = value;
                    DeputyCreationStepByUser[userId] = 4;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть шанс відвідуваності (0-100):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 4:
                    buffer.AttendanceChance = value;
                    DeputyCreationStepByUser[userId] = 5;
                    if (!DeputyCreationCountryByUser.TryGetValue(userId, out int countryId))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Не знайдено країни для створення.", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    await SendDeputyPartySelection(botClient, userId, countryId, isFast: false, cancellationToken: cancellationToken);
                    return;
                default:
                    return;
            }
        }

        private async Task SendDeputyPartySelection(ITelegramBotClient botClient, long userId, int countryId, bool isFast, CancellationToken cancellationToken)
        {
            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            string text = parties.Count == 0
                ? "У країні немає партій. Оберіть 'Без партії' для продовження."
                : "Оберіть партію для депутата:";

            string prefix = isFast
                ? "stateparliament_deputyfastcreate_party"
                : "stateparliament_deputycreate_party";

            var msg = await botClient.SendTextMessageAsync(
                userId,
                text,
                replyMarkup: StateKeyboardBuilder.DeputyPartySelectKeyboard(parties, prefix),
                cancellationToken: cancellationToken
            );
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task HandleDeputyPartySelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, DeputyCreationMode mode, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            string data = callbackQuery.Data ?? string.Empty;
            if (!TryParseTrailingId(data, out int partyId))
                return;

            if (!DeputyCreationCountryByUser.TryGetValue(userId, out int countryId))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, "Не знайдено країни для створення.", cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            if (partyId > 0)
            {
                var party = PartiesData.GetPartyById(partyId);
                if (party == null || party.CountryId != countryId)
                {
                    var errMsg = await botClient.SendTextMessageAsync(userId, "Ця партія не належить обраній країні.", cancellationToken: cancellationToken);
                    TrackOldMessage(userId, errMsg.MessageId);
                    return;
                }
            }

            if (mode == DeputyCreationMode.Manual)
            {
                if (!DeputyCreationBufferByUser.TryGetValue(userId, out var buffer))
                {
                    await botClient.SendTextMessageAsync(userId, "Немає активного створення депутата.", cancellationToken: cancellationToken);
                    return;
                }

                buffer.PartyId = partyId > 0 ? partyId : null;
                var newId = DeputiesData.AddDeputyAndReturnId(buffer);
                bool partyAssigned = false;
                if (newId.HasValue && partyId > 0)
                {
                    try
                    {
                        PartyMembersData.AddPartyMember(new PartyMemberRecord { PartyId = partyId, DeputyId = newId.Value });
                        partyAssigned = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to assign deputy to party. DeputyId={DeputyId} PartyId={PartyId} UserId={UserId}", newId.Value, partyId, userId);
                    }
                }

                ClearDeputyCreationState(userId);

                string result = newId.HasValue
                    ? $"Депутат створений з Id = {newId.Value}."
                    : "Помилка створення депутата.";
                if (newId.HasValue && partyId > 0 && !partyAssigned)
                    result += "\n⚠ Депутат створений, але не вдалося прив'язати до партії.";
                if (newId.HasValue)
                    DisableAutoParliamentStructure(countryId);

                await RefreshDeputiesMenuIfExists(botClient, userId, cancellationToken);
                CleanupOldMessagesForUser(botClient, cancellationToken, userId);
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            if (mode == DeputyCreationMode.Fast)
            {
                if (!DeputyFastCountByUser.TryGetValue(userId, out int count) || count <= 0)
                {
                    await botClient.SendTextMessageAsync(userId, "Немає активного швидкого створення.", cancellationToken: cancellationToken);
                    return;
                }

                var createdNames = new List<string>();
                int created = 0;
                int partyLinkErrors = 0;
                for (int i = 0; i < count; i++)
                {
                    var deputy = new DeputyRecord
                    {
                        CountryId = countryId,
                        PartyId = partyId > 0 ? partyId : null,
                        Name = GenerateDeputyName(),
                        Loyalty = Rng.Next(0, 101),
                        Corruption = Rng.Next(0, 101),
                        Influence = Rng.Next(0, 101),
                        AttendanceChance = Rng.Next(0, 101),
                        IsAlive = true
                    };

                    var newId = DeputiesData.AddDeputyAndReturnId(deputy);
                    if (newId.HasValue)
                    {
                        created++;
                        if (createdNames.Count < 10)
                            createdNames.Add($"{deputy.Name} #{newId.Value}");

                        if (partyId > 0)
                        {
                            try
                            {
                                PartyMembersData.AddPartyMember(new PartyMemberRecord { PartyId = partyId, DeputyId = newId.Value });
                            }
                            catch (Exception ex)
                            {
                                partyLinkErrors++;
                                _logger.LogError(ex, "Failed to assign fast-created deputy to party. DeputyId={DeputyId} PartyId={PartyId} UserId={UserId}", newId.Value, partyId, userId);
                            }
                        }
                    }
                }

                ClearDeputyCreationState(userId);

                string partyLabel = partyId > 0 ? $"партії #{partyId}" : "без партії";
                string msg = $"Створено депутатів: {created}/{count} ({partyLabel}).";
                if (createdNames.Count > 0)
                    msg += $"\nПриклади: {string.Join(", ", createdNames)}";
                if (partyLinkErrors > 0)
                    msg += $"\n⚠ Не вдалося прив'язати до партії: {partyLinkErrors}.";
                if (created > 0)
                    DisableAutoParliamentStructure(countryId);

                await RefreshDeputiesMenuIfExists(botClient, userId, cancellationToken);
                CleanupOldMessagesForUser(botClient, cancellationToken, userId);
                await botClient.SendTextMessageAsync(userId, msg, cancellationToken: cancellationToken);
            }
        }

        private static void ClearDeputyCreationState(long userId)
        {
            DeputyCreationModeByUser.Remove(userId);
            DeputyCreationStepByUser.Remove(userId);
            DeputyCreationBufferByUser.Remove(userId);
            DeputyCreationCountryByUser.Remove(userId);
            DeputyFastCountByUser.Remove(userId);
            AwaitingDeputyFastCount.Remove(userId);
        }

        private async Task RefreshDeputiesMenuIfExists(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!LastDeputiesMenuByUser.TryGetValue(userId, out var entry))
                return;

            await EditMessageInfoForDeputies(botClient, entry.ChatId, entry.MessageId, userId, cancellationToken);
        }

        private static bool TryGetCountryForDeputy(long userId, out int countryId, out string error)
        {
            error = "";
            countryId = 0;

            int? selectedCountryId = StateAdministration.GetSelectedCountryIdForUser(userId);
            int? assignedCountryId = UserData.GetActiveCharacterCountryId(userId);

            if (userId == masterID)
            {
                if (selectedCountryId.HasValue && selectedCountryId.Value > 0)
                {
                    countryId = selectedCountryId.Value;
                    return true;
                }
            }
            else
            {
                if (assignedCountryId.HasValue && assignedCountryId.Value > 0)
                {
                    countryId = assignedCountryId.Value;
                    return true;
                }

                if (selectedCountryId.HasValue && selectedCountryId.Value > 0)
                {
                    countryId = selectedCountryId.Value;
                    return true;
                }
            }

            if (!selectedCountryId.HasValue && !assignedCountryId.HasValue)
            {
                error = "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.";
                return false;
            }

            countryId = selectedCountryId ?? assignedCountryId ?? 0;
            if (countryId <= 0)
            {
                error = "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.";
                return false;
            }
            return true;
        }

        private static bool TryParsePercent(string input, out int value)
        {
            value = 0;
            if (!int.TryParse(input, out int parsed))
                return false;
            if (parsed < 0 || parsed > 100)
                return false;
            value = parsed;
            return true;
        }

        private static bool TryParseCreateLoyalDeputiesCommand(string? input, out int? count)
        {
            count = null;
            string normalized = NormalizeCommandInput(input);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            bool isCommand = LoyalDeputiesCommandPrefixes.Any(prefix =>
                normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!isCommand)
                return false;

            if (TryExtractFirstInteger(normalized, out int parsed))
                count = parsed;

            return true;
        }

        private static string NormalizeCommandInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return string.Join(" ", input
                .Trim()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool TryExtractFirstInteger(string input, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var tokens = input.Split(new[] { ' ', '\t', ',', ';', ':', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out int parsed))
                {
                    value = parsed;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseTrailingId(string data, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(data))
                return false;
            var parts = data.Split('_');
            if (parts.Length == 0)
                return false;
            return int.TryParse(parts[^1], out id);
        }

        private static string GenerateDeputyName()
        {
            NameStyle style = (NameStyle)Rng.Next(0, 4);
            Gender gender = (Gender)Rng.Next(0, 2);
            return FantasyNameGenerator.GenerateFullName(style, gender);
        }

        private static string BuildDeputyInfoText(DeputyRecord deputy)
        {
            return
                $"👤 Депутат #{deputy.DeputyId}\n" +
                $"Ім'я: {deputy.Name}\n" +
                $"Країна: {CountriesData.GetCountryNameById(deputy.CountryId)}\n" +
                $"Партія: {(deputy.PartyId.HasValue ? PartiesData.GetPartyNameById(deputy.PartyId.Value) : "Без партії")}\n" +
                $"Лояльність: {deputy.Loyalty}%\n" +
                $"Корупція: {deputy.Corruption}%\n" +
                $"Вплив: {deputy.Influence}%\n" +
                $"Шанс відвідуваності: {deputy.AttendanceChance}%\n" +
                $"Статус: {(deputy.IsAlive ? "Живий" : "Померлий")}";
        }

        private async Task ShowDeputyDismissList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.DeputyCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId)
                .ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "Оберіть депутата для звільнення:",
                StateKeyboardBuilder.DeputiesSelectKeyboard(
                    deputies,
                    selectPrefix: "stateparliament_deputydismiss",
                    pagePrefix: "stateparliament_deputydismisslist_page",
                    page: page,
                    backData: "stateparliament_deputies"
                ),
                cancellationToken
            );
        }

        private async Task HandleDeputyDismissSelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, int deputyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            var deputy = DeputiesData.GetDeputyById(deputyId);
            if (deputy == null || deputy.CountryId != countryId)
            {
                await botClient.SendTextMessageAsync(userId, "Депутат не знайдений.", cancellationToken: cancellationToken);
                return;
            }

            string text = BuildDeputyInfoText(deputy);
            if (callbackQuery.Message == null)
                return;

            await SafeEditMessageTextAsync(
                botClient,
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                text,
                StateKeyboardBuilder.DeputyDismissKeyboard(deputyId),
                cancellationToken
            );
        }

        private async Task HandleDeputyDismissAction(ITelegramBotClient botClient, CallbackQuery callbackQuery, int deputyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            var deputy = DeputiesData.GetDeputyById(deputyId);
            if (deputy == null || deputy.CountryId != countryId)
            {
                await botClient.SendTextMessageAsync(userId, "Депутат не знайдений.", cancellationToken: cancellationToken);
                return;
            }

            bool removed;
            try
            {
                RemoveDeputyDependencies(deputy.DeputyId);
                removed = DeputiesData.TryDeleteDeputy(deputy.DeputyId);
                if (removed)
                    DisableAutoParliamentStructure(countryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dismiss deputy. DeputyId={DeputyId} CountryId={CountryId} UserId={UserId}", deputy.DeputyId, countryId, userId);
                await botClient.SendTextMessageAsync(userId, "❌ Не вдалося звільнити депутата. Перевірте журнали помилок.", cancellationToken: cancellationToken);
                return;
            }

            if (!removed)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Депутата не знайдено в БД. Оновіть список і спробуйте ще раз.", cancellationToken: cancellationToken);
                return;
            }

            bool hasDeputies = DeputiesData.GetAllDeputies()
                .Any(d => d.CountryId == countryId);

            if (callbackQuery.Message == null)
                return;

            await SafeEditMessageTextAsync(
                botClient,
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                $"Депутата #{deputy.DeputyId} звільнено.",
                StateKeyboardBuilder.DeputyCommandsKeyboard(hasDeputies),
                cancellationToken
            );
        }

        private async Task HandleDeputyKillAction(ITelegramBotClient botClient, CallbackQuery callbackQuery, int deputyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            var deputy = DeputiesData.GetDeputyById(deputyId);
            if (deputy == null || deputy.CountryId != countryId)
            {
                await botClient.SendTextMessageAsync(userId, "Депутат не знайдений.", cancellationToken: cancellationToken);
                return;
            }

            deputy.IsAlive = false;
            DeputiesData.UpdateDeputy(deputy);

            string text = BuildDeputyInfoText(deputy);
            if (callbackQuery.Message == null)
                return;

            await SafeEditMessageTextAsync(
                botClient,
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                text,
                StateKeyboardBuilder.DeputyDismissKeyboard(deputyId),
                cancellationToken
            );
        }

        private async Task HandleDismissAllDeputies(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.DeputyCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId)
                .ToList();

            DisableAutoParliamentStructure(countryId);

            int dismissed = 0;
            int failed = 0;
            foreach (var deputy in deputies)
            {
                try
                {
                    RemoveDeputyDependencies(deputy.DeputyId);
                    if (DeputiesData.TryDeleteDeputy(deputy.DeputyId))
                    {
                        dismissed++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to dismiss deputy during mass dismiss. DeputyId={DeputyId} CountryId={CountryId} UserId={UserId}", deputy.DeputyId, countryId, userId);
                }
            }

            string resultText = $"Звільнено депутатів: {dismissed}.";
            if (failed > 0)
                resultText += $"\n❌ Помилок: {failed}.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                resultText,
                StateKeyboardBuilder.DeputyCommandsKeyboard(false),
                cancellationToken
            );
        }

        private async Task ShowDismissAllConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.DeputyCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var count = DeputiesData.GetAllDeputies()
                .Count(d => d.CountryId == countryId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"Ви впевнені, що хочете звільнити всіх депутатів? Кількість: {count}.",
                StateKeyboardBuilder.DeputyDismissAllConfirmKeyboard(),
                cancellationToken
            );
        }

        private static void RemoveDeputyDependencies(int deputyId)
        {
            const string cleanupSql = @"
DELETE FROM PartyMembers WHERE DeputyId = @DeputyId;
DELETE FROM Votes WHERE DeputyId = @DeputyId;
DELETE FROM SessionAttendance WHERE DeputyId = @DeputyId;";

            BaseRepository.ExecuteNonQuery(
                cleanupSql,
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId)
            );
        }

        private static void DisableAutoParliamentStructure(int countryId)
        {
            ParliamentProvisioningService.SetAutoStructureEnabled(countryId, false);
        }


        private static void TrackOldMessage(long userId, int messageId)
        {
            if (userId == 0 || messageId == 0)
                return;

            if (!OldMessages.TryGetValue(userId, out var list))
            {
                list = new List<int>();
                OldMessages[userId] = list;
            }

            list.Add(messageId);
        }
    }
}

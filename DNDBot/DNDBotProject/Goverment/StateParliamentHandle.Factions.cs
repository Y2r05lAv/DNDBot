using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private static readonly Dictionary<long, int> PartyCreationStepByUser = new();
        private static readonly Dictionary<long, PartyRecord> PartyCreationBufferByUser = new();
        private static readonly Dictionary<long, int> PartyCreationCountryByUser = new();
        private static readonly Dictionary<long, int> PartyEditStepByUser = new();
        private static readonly Dictionary<long, PartyRecord> PartyEditBufferByUser = new();
        private static readonly Dictionary<long, int> PartyEditCountryByUser = new();

        private async Task EditMessageInfoForFaction(ITelegramBotClient botClient, long chatId, int msgId, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            int count = PartiesData.GetAllParties().Count(p => p.CountryId == countryId);
            string text = $"🏳 Фракції / партії\n\nКількість партій у країні: {count}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.FactionCommandsKeyboard(count > 0),
                cancellationToken
            );
        }

        private async Task StartPartyCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            ClearPartyCreationState(userId);
            ClearPartyEditState(userId);
            PartyCreationCountryByUser[userId] = countryId;
            PartyCreationStepByUser[userId] = 0;
            PartyCreationBufferByUser[userId] = new PartyRecord
            {
                CountryId = countryId,
                Name = string.Empty,
                Ideology = string.Empty,
                Discipline = 50,
                GovernmentAligned = false,
                Influence = 50
            };

            var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть назву партії:", cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task HandlePartyCreationInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!PartyCreationStepByUser.TryGetValue(userId, out int step) || !PartyCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            TrackOldMessage(userId, message.MessageId);

            string input = (message.Text ?? string.Empty).Trim();

            switch (step)
            {
                case 0:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Назва не може бути порожньою. Спробуйте ще раз:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.Name = input;
                    PartyCreationStepByUser[userId] = 1;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть ідеологію (можна залишити порожньо):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 1:
                    buffer.Ideology = string.IsNullOrWhiteSpace(input) ? string.Empty : input;
                    PartyCreationStepByUser[userId] = 2;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть рівень дисципліни (0-100):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 2:
                    if (!TryParsePercent(input, out int discipline))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 0 до 100:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.Discipline = discipline;
                    PartyCreationStepByUser[userId] = 3;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Партія провладна? (так/ні або 1/0):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 3:
                    if (!TryParseBoolSimple(input, out bool aligned))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть так/ні або 1/0:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.GovernmentAligned = aligned;
                    PartyCreationStepByUser[userId] = 4;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть рівень впливу (0-100):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 4:
                    if (!TryParsePercent(input, out int influence))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 0 до 100:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.Influence = influence;
                    break;
                default:
                    return;
            }

            string result;
            try
            {
                result = PartiesData.AddParty(buffer);
            }
            catch (Exception ex)
            {
                int countryId = PartyCreationCountryByUser.TryGetValue(userId, out int cId) ? cId : 0;
                _logger.LogError(ex, "Create party failed. UserId={UserId} CountryId={CountryId}", userId, countryId);
                result = $"Помилка створення партії: {ex.Message}";
            }

            ClearPartyCreationState(userId);
            CleanupOldMessagesForUser(botClient, cancellationToken, userId);
            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }

        private async Task StartPartyEditSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            if (parties.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "У країні немає партій для редагування.",
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "Оберіть фракцію для редагування:",
                StateKeyboardBuilder.FactionSelectKeyboard(parties, "stateparliament_factionedit_pick"),
                cancellationToken
            );
        }

        private async Task HandlePartyEditSelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, int partyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            var party = PartiesData.GetPartyById(partyId);
            if (party == null || party.CountryId != countryId)
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, "Партію не знайдено або вона належить іншій країні.", cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            ClearPartyCreationState(userId);
            ClearPartyEditState(userId);

            PartyEditCountryByUser[userId] = countryId;
            PartyEditStepByUser[userId] = 0;
            PartyEditBufferByUser[userId] = new PartyRecord
            {
                PartyId = party.PartyId,
                CountryId = party.CountryId,
                Name = party.Name,
                Ideology = party.Ideology,
                Discipline = party.Discipline,
                GovernmentAligned = party.GovernmentAligned,
                Influence = party.Influence
            };

            var msg = await botClient.SendTextMessageAsync(
                userId,
                BuildPartyEditPrompt(0, party),
                cancellationToken: cancellationToken
            );
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task HandlePartyEditInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!PartyEditStepByUser.TryGetValue(userId, out int step) || !PartyEditBufferByUser.TryGetValue(userId, out var buffer))
                return;

            TrackOldMessage(userId, message.MessageId);
            string input = (message.Text ?? string.Empty).Trim();
            bool keepCurrent = IsKeepPartyValueInput(input);

            switch (step)
            {
                case 0:
                    if (!keepCurrent)
                    {
                        if (string.IsNullOrWhiteSpace(input))
                        {
                            var errMsg = await botClient.SendTextMessageAsync(userId, "Назва не може бути порожньою. Введіть назву або '-' щоб лишити поточну.", cancellationToken: cancellationToken);
                            TrackOldMessage(userId, errMsg.MessageId);
                            return;
                        }

                        buffer.Name = input;
                    }

                    PartyEditStepByUser[userId] = 1;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, BuildPartyEditPrompt(1, buffer), cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 1:
                    if (!keepCurrent)
                        buffer.Ideology = string.IsNullOrWhiteSpace(input) ? string.Empty : input;

                    PartyEditStepByUser[userId] = 2;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, BuildPartyEditPrompt(2, buffer), cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 2:
                    if (!keepCurrent)
                    {
                        if (!TryParsePercent(input, out int discipline))
                        {
                            var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 0 до 100 або '-'.", cancellationToken: cancellationToken);
                            TrackOldMessage(userId, errMsg.MessageId);
                            return;
                        }

                        buffer.Discipline = discipline;
                    }

                    PartyEditStepByUser[userId] = 3;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, BuildPartyEditPrompt(3, buffer), cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 3:
                    if (!keepCurrent)
                    {
                        if (!TryParseBoolSimple(input, out bool aligned))
                        {
                            var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть так/ні, 1/0 або '-'.", cancellationToken: cancellationToken);
                            TrackOldMessage(userId, errMsg.MessageId);
                            return;
                        }

                        buffer.GovernmentAligned = aligned;
                    }

                    PartyEditStepByUser[userId] = 4;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, BuildPartyEditPrompt(4, buffer), cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case 4:
                    if (!keepCurrent)
                    {
                        if (!TryParsePercent(input, out int influence))
                        {
                            var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть число від 0 до 100 або '-'.", cancellationToken: cancellationToken);
                            TrackOldMessage(userId, errMsg.MessageId);
                            return;
                        }

                        buffer.Influence = influence;
                    }
                    break;
                default:
                    return;
            }

            string result;
            try
            {
                result = PartiesData.UpdateParty(buffer);
            }
            catch (Exception ex)
            {
                int countryId = PartyEditCountryByUser.TryGetValue(userId, out int cId) ? cId : 0;
                _logger.LogError(ex, "Update party failed. UserId={UserId} CountryId={CountryId} PartyId={PartyId}", userId, countryId, buffer.PartyId);
                result = $"Помилка оновлення партії: {ex.Message}";
            }

            ClearPartyEditState(userId);
            CleanupOldMessagesForUser(botClient, cancellationToken, userId);
            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }

        private async Task StartPartyDeleteSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            if (parties.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "У країні немає партій для видалення.",
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "Оберіть фракцію для видалення:",
                StateKeyboardBuilder.FactionSelectKeyboard(parties, "stateparliament_factiondelete_pick", "stateparliament_faction"),
                cancellationToken
            );
        }

        private async Task HandlePartyDeleteSelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, int partyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            long chatId = callbackQuery.Message?.Chat.Id ?? userId;
            int msgId = callbackQuery.Message?.MessageId ?? 0;

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var party = PartiesData.GetPartyById(partyId);
            if (party == null || party.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Партію не знайдено або вона належить іншій країні.",
                    StateKeyboardBuilder.FactionCommandsKeyboard(PartiesData.GetAllParties().Any(p => p.CountryId == countryId)),
                    cancellationToken
                );
                return;
            }

            string text =
$@"⚠ Підтвердження видалення фракції

{BuildPartyShortInfo(party)}

Після видалення всі депутати цієї фракції стануть ""Без партії"".";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.FactionDeleteConfirmKeyboard(party.PartyId),
                cancellationToken
            );
        }

        private async Task HandlePartyDeleteConfirm(ITelegramBotClient botClient, CallbackQuery callbackQuery, int partyId, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            long chatId = callbackQuery.Message?.Chat.Id ?? userId;
            int msgId = callbackQuery.Message?.MessageId ?? 0;

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var party = PartiesData.GetPartyById(partyId);
            if (party == null || party.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Партію не знайдено або вона вже видалена.",
                    StateKeyboardBuilder.FactionCommandsKeyboard(PartiesData.GetAllParties().Any(p => p.CountryId == countryId)),
                    cancellationToken
                );
                return;
            }

            ClearPartyCreationState(userId);
            ClearPartyEditState(userId);

            string result;
            try
            {
                RemovePartyDependencies(party.PartyId);
                result = PartiesData.DeleteParty(party.PartyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete party failed. UserId={UserId} CountryId={CountryId} PartyId={PartyId}", userId, countryId, party.PartyId);
                result = $"Помилка видалення партії: {ex.Message}";
            }

            bool hasParties = PartiesData.GetAllParties().Any(p => p.CountryId == countryId);
            string text = $"{result}\n\nФракція: #{party.PartyId} {party.Name}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.FactionCommandsKeyboard(hasParties),
                cancellationToken
            );
        }

        private async Task ShowPartiesList(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            if (parties.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "У країні немає партій.",
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var lines = new List<string> { "🏳 Список партій:" };
            foreach (var p in parties)
            {
                string name = string.IsNullOrWhiteSpace(p.Name) ? $"#{p.PartyId}" : p.Name;
                string ideology = string.IsNullOrWhiteSpace(p.Ideology) ? "—" : p.Ideology;
                string aligned = p.GovernmentAligned ? "так" : "ні";
                lines.Add($"#{p.PartyId} {name} | ідеологія: {ideology} | дисципліна: {p.Discipline}% | вплив: {p.Influence}% | провладна: {aligned}");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                string.Join("\n", lines),
                StateKeyboardBuilder.FactionCommandsKeyboard(true),
                cancellationToken
            );
        }

        private async Task ShowPartyMembers(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.FactionCommandsKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var parties = PartiesData.GetAllParties()
                .Where(p => p.CountryId == countryId)
                .ToList();

            var deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId)
                .ToList();

            var membersByParty = new Dictionary<int, List<DeputyRecord>>();
            foreach (var deputy in deputies)
            {
                if (!deputy.PartyId.HasValue)
                    continue;

                int partyId = deputy.PartyId.Value;
                if (!membersByParty.TryGetValue(partyId, out var list))
                {
                    list = new List<DeputyRecord>();
                    membersByParty[partyId] = list;
                }
                list.Add(deputy);
            }

            var noParty = deputies.Where(d => !d.PartyId.HasValue).ToList();

            var lines = new List<string> { "👥 Склад партій:" };
            if (parties.Count == 0 && noParty.Count == 0)
            {
                lines.Add("У країні немає депутатів.");
            }
            else
            {
                foreach (var party in parties.OrderBy(p => p.PartyId))
                {
                    string name = string.IsNullOrWhiteSpace(party.Name) ? $"#{party.PartyId}" : party.Name;
                    lines.Add($"🏳 {name} (#{party.PartyId})");

                    if (membersByParty.TryGetValue(party.PartyId, out var list) && list.Count > 0)
                    {
                        foreach (var deputy in list.OrderBy(d => d.DeputyId))
                        {
                            lines.Add($"- {deputy.Name} #{deputy.DeputyId}");
                        }
                    }
                    else
                    {
                        lines.Add("- Немає депутатів у партії.");
                    }
                }

                if (noParty.Count > 0)
                {
                    lines.Add("🏳 Без партії");
                    foreach (var deputy in noParty.OrderBy(d => d.DeputyId))
                    {
                        lines.Add($"- {deputy.Name} #{deputy.DeputyId}");
                    }
                }
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                string.Join("\n", lines),
                StateKeyboardBuilder.FactionCommandsKeyboard(parties.Count > 0),
                cancellationToken
            );
        }

        private static void ClearPartyCreationState(long userId)
        {
            PartyCreationStepByUser.Remove(userId);
            PartyCreationBufferByUser.Remove(userId);
            PartyCreationCountryByUser.Remove(userId);
        }

        private static void ClearPartyEditState(long userId)
        {
            PartyEditStepByUser.Remove(userId);
            PartyEditBufferByUser.Remove(userId);
            PartyEditCountryByUser.Remove(userId);
        }

        private static bool IsKeepPartyValueInput(string input)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            return s == "-" ||
                   s == "залишити" ||
                   s == "пропустити" ||
                   s == "пропустить" ||
                   s == "skip";
        }

        private static string BuildPartyEditPrompt(int step, PartyRecord party)
        {
            const string keepHint = "Введіть '-' щоб залишити поточне значення.";
            return step switch
            {
                0 => $"Редагування фракції #{party.PartyId}\nПоточна назва: {party.Name}\nВкажіть нову назву.\n{keepHint}",
                1 => $"Поточна ідеологія: {FormatPartyIdeology(party.Ideology)}\nВкажіть нову ідеологію (можна порожньо).\n{keepHint}",
                2 => $"Поточна дисципліна: {party.Discipline}%\nВкажіть нову дисципліну (0-100).\n{keepHint}",
                3 => $"Поточна провладність: {(party.GovernmentAligned ? "так" : "ні")}\nПартія провладна? (так/ні або 1/0).\n{keepHint}",
                4 => $"Поточний вплив: {party.Influence}%\nВкажіть новий вплив (0-100).\n{keepHint}",
                _ => "Вкажіть нове значення."
            };
        }

        private static string FormatPartyIdeology(string ideology)
        {
            return string.IsNullOrWhiteSpace(ideology) ? "—" : ideology;
        }

        private static string BuildPartyShortInfo(PartyRecord party)
        {
            string aligned = party.GovernmentAligned ? "так" : "ні";
            return $"#{party.PartyId} {party.Name}\nІдеологія: {FormatPartyIdeology(party.Ideology)}\nДисципліна: {party.Discipline}%\nВплив: {party.Influence}%\nПровладна: {aligned}";
        }

        private static void RemovePartyDependencies(int partyId)
        {
            const string cleanupSql = @"
UPDATE Deputies
SET PartyId = NULL
WHERE PartyId = @PartyId;

DELETE FROM PartyMembers
WHERE PartyId = @PartyId;";

            BaseRepository.ExecuteNonQuery(
                cleanupSql,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId)
            );
        }

        private static bool TryParseBoolSimple(string input, out bool value)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (s == "1" || s == "true" || s == "так" || s == "yes")
            {
                value = true;
                return true;
            }
            if (s == "0" || s == "false" || s == "ні" || s == "no")
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }
    }
}

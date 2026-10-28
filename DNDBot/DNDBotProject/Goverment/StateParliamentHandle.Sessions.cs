using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private async Task EditMessageInfoForConveneParl(ITelegramBotClient botClient, long chatId, int msgId, long userId, CancellationToken cancellationToken)
        {
            var msg = await botClient.SendTextMessageAsync(masterID, "Гравці хочуть скликати парламент! Очікування кворума чи відкликання заяви...", cancellationToken: cancellationToken, replyMarkup: StateKeyboardBuilder.CollectPairlamentQuorumKeyboard());
            await botClient.EditMessageTextAsync(chatId, msgId, text: "🪧 Скликати парламент: Очікування кворума", cancellationToken: cancellationToken);
            msgIdForParliamentSession = msg.MessageId;
        }

        private async Task CallParlaimentSession(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            int? countryId = userId == masterID
                ? StateAdministration.GetSelectedCountryIdForUser(userId)
                : UserData.GetActiveCharacterCountryId(userId);

            if (!countryId.HasValue)
            {
                await botClient.SendTextMessageAsync(
                    masterID,
                    "Не знайдено країни для скликання. Оберіть країну в адміністрації або призначте CountryId персонажу.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            PendingSessionCountryByUser[userId] = countryId.Value;

            int requiredQuorum = QuorumPercentByUser.TryGetValue(userId, out int quorum) ? quorum : 0;
            await TrySendToParliamentGroupAsync(
                botClient,
                $"Парламент скликано з необхідним кворумом {requiredQuorum}%. Депутати починають приходити...",
                cancellationToken,
                notifyMasterOnFailure: true
            );
            var msg = await TrySendToParliamentGroupAsync(
                botClient,
                "Очікування на депутатів...",
                cancellationToken,
                notifyMasterOnFailure: false
            );
            msgIdForParliamentSession = msg?.MessageId ?? 0;

            await botClient.SendTextMessageAsync(
                masterID,
                "Чи всі депутати прийшли до парламенту? Якщо так, натисніть 'Всі прийшли'. Якщо ні — натисніть 'Порахувати'.",
                cancellationToken: cancellationToken,
                replyMarkup: StateKeyboardBuilder.ParliamentSessionStartKeyboard()
            );
        }

        private async Task StartQuorumPercentInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
                return;

            AwaitingQuorumPercent[userId] = true;
            await botClient.SendTextMessageAsync(
                userId,
                "Вкажіть потрібний відсоток кворуму (0-100):",
                cancellationToken: cancellationToken
            );
        }

        private async Task NotifyLeaveQuorum(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            if (ParliamentGroupChatId == 0)
            {
                await botClient.SendTextMessageAsync(masterID, "Групу для повідомлень про парламент не знайдено. Спочатку напишіть боту у групі.", cancellationToken: cancellationToken);
                return;
            }

            await TrySendToParliamentGroupAsync(
                botClient,
                "Позив до парламенту відхилено.",
                cancellationToken,
                notifyMasterOnFailure: true
            );
            DeleteMSG(botClient, msgIdForParliamentSession, masterID, cancellationToken);
        }

        private async Task ResolveSessionStart(ITelegramBotClient botClient, long userId, bool allPresent, CancellationToken cancellationToken)
        {
            if (userId != masterID)
                return;

            if (!PendingSessionCountryByUser.TryGetValue(userId, out int countryId))
            {
                await botClient.SendTextMessageAsync(masterID, "Немає активної сесії для старту.", cancellationToken: cancellationToken);
                return;
            }

            var deputies = DeputiesData
                .GetAllDeputies()
                .Where(d => d.CountryId == countryId && d.IsAlive)
                .ToList();

            if (deputies.Count == 0)
            {
                await botClient.SendTextMessageAsync(masterID, "Немає депутатів для цієї країни.", cancellationToken: cancellationToken);
                return;
            }

            var present = new List<DeputyRecord>();
            var absent = new List<DeputyRecord>();

            foreach (var d in deputies)
            {
                bool isPresent;
                if (allPresent)
                {
                    isPresent = true;
                }
                else
                {
                    int chance = Math.Clamp(d.AttendanceChance, 0, 100);
                    int roll = Rng.Next(1, 101);
                    isPresent = roll <= chance;
                }

                if (isPresent)
                    present.Add(d);
                else
                    absent.Add(d);
            }

            int total = deputies.Count;
            int presentCount = present.Count;
            int requiredPercent = QuorumPercentByUser.TryGetValue(userId, out var qp) ? qp : 0;
            int requiredCount = (int)Math.Ceiling(total * (requiredPercent / 100.0));
            bool quorumOk = presentCount >= requiredCount;

            int sessionId = AddSessionAndReturnId(new ParliamentSessionRecord
            {
                CountryId = countryId,
                CalledByCharacterId = UserData.GetActiveCharacterId(userId),
                Status = quorumOk ? "Active" : "FailedQuorum",
                RequiredQuorum = requiredCount,
                PresentCount = presentCount,
                StartTime = DateTime.Now,
                EndTime = quorumOk ? null : DateTime.Now
            });

            if (sessionId > 0)
            {
                foreach (var d in present)
                {
                    SessionAttendanceData.AddAttendance(new SessionAttendanceRecord
                    {
                        SessionId = sessionId,
                        DeputyId = d.DeputyId,
                        IsPresent = true,
                        Reason = "present"
                    });
                }

                foreach (var d in absent)
                {
                    SessionAttendanceData.AddAttendance(new SessionAttendanceRecord
                    {
                        SessionId = sessionId,
                        DeputyId = d.DeputyId,
                        IsPresent = false,
                        Reason = "absent"
                    });
                }

                if (quorumOk)
                    ActiveParliamentSessionByCountry[countryId] = sessionId;
            }

            string resultText = BuildSessionSummaryText(sessionId, presentCount, total, requiredCount, requiredPercent, quorumOk);
            InlineKeyboardMarkup attendanceKeyboard = sessionId > 0
                ? StateKeyboardBuilder.ParliamentSessionAttendanceActionsKeyboard(sessionId)
                : null;

            if (ParliamentGroupChatId != 0)
            {
                await SafeEditOrSendAsync(
                    botClient,
                    ParliamentGroupChatId,
                    msgIdForParliamentSession,
                    resultText,
                    attendanceKeyboard,
                    cancellationToken
                );
            }

            await botClient.SendTextMessageAsync(
                masterID,
                resultText,
                replyMarkup: attendanceKeyboard,
                cancellationToken: cancellationToken
            );
        }

        private static string BuildSessionSummaryText(int sessionId, int presentCount, int total, int requiredCount, int requiredPercent, bool quorumOk)
        {
            return
$@"🪧 Сесія парламенту #{sessionId}
Присутні: {presentCount}/{total} ({(total > 0 ? (int)Math.Round(100.0 * presentCount / total) : 0)}%)
Кворум: потрібно {requiredCount} ({requiredPercent}%) — {(quorumOk ? "виконано ✅" : "не виконано ❌")}

Оберіть кнопкою, який список депутатів показати.";
        }

        private async Task HandleSessionAttendanceCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, string data, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null)
                return;

            long chatId = callbackQuery.Message.Chat.Id;
            int msgId = callbackQuery.Message.MessageId;

            if (data.StartsWith("stateparliament_sessionattendance_summary_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int summarySessionId) && summarySessionId > 0)
                {
                    await ShowSessionAttendanceSummary(botClient, chatId, msgId, summarySessionId, cancellationToken);
                }
                return;
            }

            var parts = data.Split('_');
            if (parts.Length < 5)
                return;

            bool showPresent;
            if (string.Equals(parts[2], "present", StringComparison.OrdinalIgnoreCase))
            {
                showPresent = true;
            }
            else if (string.Equals(parts[2], "absent", StringComparison.OrdinalIgnoreCase))
            {
                showPresent = false;
            }
            else
            {
                return;
            }

            if (!int.TryParse(parts[3], out int sessionId) || sessionId <= 0)
                return;
            if (!int.TryParse(parts[4], out int page))
                page = 0;

            await ShowSessionAttendanceList(botClient, chatId, msgId, sessionId, showPresent, page, cancellationToken);
        }

        private async Task ShowSessionAttendanceSummary(ITelegramBotClient botClient, long chatId, int msgId, int sessionId, CancellationToken cancellationToken)
        {
            var session = ParliamentSessionsData.GetSessionById(sessionId);
            if (session == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Сесію не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var attendance = SessionAttendanceData.GetAllAttendance()
                .Where(a => a.SessionId == sessionId)
                .ToList();

            int total = attendance.Count;
            int presentCount = attendance.Count(a => a.IsPresent);
            int requiredCount = Math.Max(0, session.RequiredQuorum);
            bool quorumOk = presentCount >= requiredCount;
            int requiredPercent = total > 0
                ? (int)Math.Round(100.0 * requiredCount / total)
                : 0;

            string resultText = BuildSessionSummaryText(sessionId, presentCount, total, requiredCount, requiredPercent, quorumOk);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                resultText,
                StateKeyboardBuilder.ParliamentSessionAttendanceActionsKeyboard(sessionId),
                cancellationToken
            );
        }

        private async Task ShowSessionAttendanceList(ITelegramBotClient botClient, long chatId, int msgId, int sessionId, bool showPresent, int page, CancellationToken cancellationToken)
        {
            var session = ParliamentSessionsData.GetSessionById(sessionId);
            if (session == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Сесію не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var attendance = SessionAttendanceData.GetAllAttendance()
                .Where(a => a.SessionId == sessionId && a.IsPresent == showPresent)
                .ToList();

            var deputyIds = attendance
                .Select(a => a.DeputyId)
                .Distinct()
                .ToHashSet();

            var deputyMap = DeputiesData.GetAllDeputies()
                .Where(d => deputyIds.Contains(d.DeputyId))
                .ToDictionary(d => d.DeputyId, d => d);

            var entries = attendance
                .Select(a =>
                {
                    if (deputyMap.TryGetValue(a.DeputyId, out var deputy) && !string.IsNullOrWhiteSpace(deputy.Name))
                        return $"{deputy.Name} (#{a.DeputyId})";
                    return $"#{a.DeputyId}";
                })
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            const int perPage = 20;
            int total = entries.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 0, totalPages - 1);

            var pageItems = entries
                .Skip(page * perPage)
                .Take(perPage)
                .ToList();

            string title = showPresent ? "✅ Хто прийшов" : "❌ Хто не прийшов";
            var sb = new StringBuilder();
            sb.AppendLine($"🪧 Сесія парламенту #{sessionId}");
            sb.AppendLine(title);
            sb.AppendLine($"Усього: {total}");

            if (pageItems.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Список порожній.");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < pageItems.Count; i++)
                {
                    int number = (page * perPage) + i + 1;
                    sb.AppendLine($"{number}. {pageItems[i]}");
                }
                sb.AppendLine();
                sb.AppendLine($"Сторінка {page + 1}/{totalPages}");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString().TrimEnd(),
                StateKeyboardBuilder.ParliamentSessionAttendanceListKeyboard(sessionId, showPresent, page, totalPages),
                cancellationToken
            );
        }


        private async Task CloseActiveParliamentSession(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!ActiveParliamentSessionByCountry.TryGetValue(countryId, out int sessionId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "🏛 Функції парламенту\nНемає активної сесії для закриття.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var session = ParliamentSessionsData.GetSessionById(sessionId);
            if (session != null)
            {
                session.Status = "Closed";
                session.EndTime = DateTime.Now;
                ParliamentSessionsData.UpdateSession(session);
            }

            ActiveParliamentSessionByCountry.Remove(countryId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "Сесію парламенту закрито.",
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private static int AddSessionAndReturnId(ParliamentSessionRecord session)
        {
            const string insert = @"INSERT INTO ParliamentSessions (CountryId, CalledByCharacterId, Status, RequiredQuorum, PresentCount, StartTime, EndTime)
VALUES (@CountryId, @CalledByCharacterId, @Status, @RequiredQuorum, @PresentCount, @StartTime, @EndTime);
    SELECT CAST(SCOPE_IDENTITY() AS INT)"; 

    
            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", System.Data.SqlDbType.Int, session.CountryId),
                BaseRepository.Param("@CalledByCharacterId", System.Data.SqlDbType.Int, session.CalledByCharacterId),
                BaseRepository.Param("@Status", System.Data.SqlDbType.NVarChar, session.Status, 20),
                BaseRepository.Param("@RequiredQuorum", System.Data.SqlDbType.Int, session.RequiredQuorum),
                BaseRepository.Param("@PresentCount", System.Data.SqlDbType.Int, session.PresentCount),
                BaseRepository.Param("@StartTime", System.Data.SqlDbType.DateTime2, session.StartTime),
                BaseRepository.Param("@EndTime", System.Data.SqlDbType.DateTime2, session.EndTime)
            );

            if (idObj == null || idObj == DBNull.Value)
                return 0;

            return Convert.ToInt32(idObj);
        }

        private static bool IsParliamentChatUnavailableError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.Contains("chat not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("bot was kicked", StringComparison.OrdinalIgnoreCase)
                || message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                || message.Contains("chat is deactivated", StringComparison.OrdinalIgnoreCase)
                || message.Contains("group chat was deleted", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Message> TrySendToParliamentGroupAsync(
            ITelegramBotClient botClient,
            string text,
            CancellationToken cancellationToken,
            bool notifyMasterOnFailure)
        {
            if (ParliamentGroupChatId == 0)
            {
                if (notifyMasterOnFailure)
                {
                    await botClient.SendTextMessageAsync(
                        masterID,
                        "⚠ Парламентська група не задана. Напишіть боту у групі, щоб прив'язати чат.",
                        cancellationToken: cancellationToken
                    );
                }
                return null;
            }

            try
            {
                return await botClient.SendTextMessageAsync(
                    ParliamentGroupChatId,
                    text,
                    cancellationToken: cancellationToken
                );
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (IsParliamentChatUnavailableError(ex.Message))
            {
                _logger.LogWarning(ex, "Parliament group chat unavailable. ChatId={ChatId} ErrorCode={ErrorCode} Error={Error}",
                    ParliamentGroupChatId, ex.ErrorCode, ex.Message);

                long brokenChatId = ParliamentGroupChatId;
                ParliamentGroupChatId = 0;

                if (notifyMasterOnFailure)
                {
                    await botClient.SendTextMessageAsync(
                        masterID,
                        $"⚠ Не вдалося відправити у парламентську групу ({brokenChatId}). Причина: {ex.Message}",
                        cancellationToken: cancellationToken
                    );
                }

                return null;
            }
        }

        private async Task SafeEditOrSendAsync(
            ITelegramBotClient botClient,
            long chatId,
            int messageId,
            string text,
            InlineKeyboardMarkup replyMarkup,
            CancellationToken cancellationToken)
        {
            try
            {
                if (messageId > 0)
                {
                    await botClient.EditMessageTextAsync(
                        chatId,
                        messageId,
                        text: text,
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken
                    );
                    return;
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.Message != null && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                if (IsParliamentChatUnavailableError(ex.Message))
                {
                    _logger.LogWarning(ex, "SafeEditOrSendAsync chat unavailable. ChatId={ChatId} MessageId={MessageId}", chatId, messageId);
                    if (chatId == ParliamentGroupChatId)
                        ParliamentGroupChatId = 0;
                    await botClient.SendTextMessageAsync(masterID, "⚠ Парламентський чат недоступний. Повідомлення у групу не доставлено.", cancellationToken: cancellationToken);
                    return;
                }

                bool fallbackToSend = ex.Message != null &&
                    (ex.Message.Contains("message to edit not found", StringComparison.OrdinalIgnoreCase) ||
                     ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase));

                if (!fallbackToSend)
                {
                    _logger.LogError(ex, "SafeEditOrSendAsync failed. ChatId={ChatId} MessageId={MessageId} ErrorCode={ErrorCode} Error={Error}",
                        chatId, messageId, ex.ErrorCode, ex.Message);
                    throw;
                }
            }

            try
            {
                await botClient.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (IsParliamentChatUnavailableError(ex.Message))
            {
                _logger.LogWarning(ex, "SafeEditOrSendAsync fallback send chat unavailable. ChatId={ChatId}", chatId);
                if (chatId == ParliamentGroupChatId)
                    ParliamentGroupChatId = 0;
                await botClient.SendTextMessageAsync(masterID, "⚠ Парламентський чат недоступний. Результат сесії надіслано тільки майстру.", cancellationToken: cancellationToken);
            }
        }
    }
}


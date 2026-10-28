using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private async Task SafeAnswerCallbackQueryAsync(ITelegramBotClient botClient, string callbackQueryId, string text = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId, text, cancellationToken: cancellationToken);
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.Message != null &&
                    (ex.Message.Contains("query is too old", StringComparison.OrdinalIgnoreCase) ||
                     ex.Message.Contains("query ID is invalid", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _logger.LogError(ex, "SafeAnswerCallbackQueryAsync failed. CallbackQueryId={CallbackQueryId} ErrorCode={ErrorCode} Error={Error}",
                    callbackQueryId, ex.ErrorCode, ex.Message);
                throw;
            }
        }

        private static bool TryParseLastInt(string data, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(data))
                return false;
            var parts = data.Split('_');
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (int.TryParse(parts[i], out id))
                    return true;
            }
            id = 0;
            return false;
        }

        private static bool TryParseDeputyInfoCallback(string data, out int deputyId)
        {
            deputyId = 0;
            const string prefix = "stateparliament_deputy_";
            if (string.IsNullOrWhiteSpace(data) || !data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string tail = data.Substring(prefix.Length);
            return int.TryParse(tail, out deputyId);
        }

        private static void CleanupOldMessagesForUser(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            if (userId == 0)
                return;

            if (!OldMessages.TryGetValue(userId, out var list))
                return;

            DeleteAllOldMessages(botClient, cancellationToken, userId);
            list.Clear();
        }
    }
}

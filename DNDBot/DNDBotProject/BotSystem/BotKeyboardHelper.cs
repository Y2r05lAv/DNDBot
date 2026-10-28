#nullable enable

using System;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.BotSystem
{
    public static class BotKeyboardHelper
    {
        public static bool UseInlineButtonsUnderMessages { get; set; } = false;

        public static bool UseMinimalReplyKeyboard { get; set; } = true;

        public static InlineKeyboardMarkup? ConvertReplyKeyboardToInline(ReplyKeyboardMarkup? replyKeyboard)
        {
            if (replyKeyboard == null)
                return null;

            var rows = replyKeyboard.Keyboard?.Select(row =>
                row.Select(btn =>
                    InlineKeyboardButton.WithCallbackData(btn.Text ?? string.Empty, "kb_" + EscapeCallback(btn.Text ?? string.Empty))
                ).ToArray()
            ).ToArray();

            if (rows == null || rows.Length == 0)
                return null;

            return new InlineKeyboardMarkup(rows);
        }

        private static string EscapeCallback(string text)
        {
            return text.Trim().Replace("_", "__");
        }

        public static string UnescapeCallback(string callbackData)
        {
            if (callbackData.StartsWith("kb_"))
                callbackData = callbackData.Substring(3);

            return callbackData.Replace("__", "_");
        }
    }
}

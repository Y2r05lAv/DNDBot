using System.Text;

namespace DNDBot.BotSystem
{
    internal static class LogTags
    {
        public const string System = "SYSTEM";
        public const string Cmd = "CMD";
        public const string Data = "DATA";
        public const string DbClear = "DB-CLEAR";
        public const string Broadcast = "BROADCAST";
        public const string Fail = "FAIL";
        public const string Warn = "WARN";

        public static string Tag(string message, params string[] tags)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            if (tags == null || tags.Length == 0)
                return message;

            var sb = new StringBuilder();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                sb.Append('[').Append(tag).Append(']');
            }

            if (sb.Length > 0)
                sb.Append(' ');

            sb.Append(message);
            return sb.ToString();
        }
    }
}

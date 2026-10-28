#nullable enable
using System;

namespace DNDBot.Goverment
{
    internal static class CountryNameUtils
    {
        internal const int MaxLength = 100;

        internal static bool TryNormalize(string? input, out string name, out string error)
        {
            name = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Назва не може бути порожньою.";
                return false;
            }

            string trimmed = input.Trim();
            trimmed = trimmed.Replace('\r', ' ').Replace('\n', ' ');
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            trimmed = string.Join(" ", parts);

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                error = "Назва не може бути порожньою.";
                return false;
            }

            if (trimmed.Length > MaxLength)
            {
                error = $"Назва занадто довга (>{MaxLength} символів).";
                return false;
            }

            name = trimmed;
            return true;
        }
    }
}

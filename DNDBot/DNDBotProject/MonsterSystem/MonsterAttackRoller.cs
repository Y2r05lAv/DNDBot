using DNDBot.BotMath;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DNDBot.MonsterSystem
{
    internal static class MonsterAttackRoller
    {
        private static readonly Regex TokenRegex = new Regex(
            @"[+-]?\s*\d+\s*d\s*\d+|[+-]?\s*\d+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidDamageExpression(string expression, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "Порожній вираз урону.";
                return false;
            }

            var tokens = TokenRegex.Matches(expression);
            if (tokens.Count == 0)
            {
                error = "Вираз урону має бути у форматі на кшталт '2d6 + 3'.";
                return false;
            }

            foreach (Match token in tokens)
            {
                string normalized = NormalizeToken(token.Value);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                int sign = 1;
                if (normalized[0] == '+')
                    normalized = normalized[1..];
                else if (normalized[0] == '-')
                {
                    sign = -1;
                    normalized = normalized[1..];
                }

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    error = "Некоректний токен у виразі урону.";
                    return false;
                }

                if (normalized.Contains('d', StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = normalized.Split('d', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2 ||
                        !int.TryParse(parts[0], out int count) ||
                        !int.TryParse(parts[1], out int sides))
                    {
                        error = $"Некоректний кубик: {token.Value}.";
                        return false;
                    }

                    if (count <= 0 || sides <= 1)
                    {
                        error = $"Некоректний кубик: {token.Value}.";
                        return false;
                    }

                    if (count > 200 || sides > 1000)
                    {
                        error = "Завелике значення кубиків (максимум 200d1000).";
                        return false;
                    }
                }
                else
                {
                    if (!int.TryParse((sign < 0 ? "-" : "") + normalized, out _))
                    {
                        error = $"Некоректне число: {token.Value}.";
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryRollDamage(string expression, out int damage, out string breakdown, out string error)
        {
            damage = 0;
            breakdown = string.Empty;
            error = string.Empty;

            if (!IsValidDamageExpression(expression, out error))
                return false;

            var details = new List<string>();
            int total = 0;

            foreach (Match token in TokenRegex.Matches(expression))
            {
                string normalized = NormalizeToken(token.Value);
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                int sign = 1;
                if (normalized[0] == '+')
                    normalized = normalized[1..];
                else if (normalized[0] == '-')
                {
                    sign = -1;
                    normalized = normalized[1..];
                }

                if (normalized.Contains('d', StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = normalized.Split('d', StringSplitOptions.RemoveEmptyEntries);
                    int count = int.Parse(parts[0]);
                    int sides = int.Parse(parts[1]);
                    int subtotal = 0;
                    var rolls = new List<int>(count);

                    for (int i = 0; i < count; i++)
                    {
                        int roll = MathfDND.DiceReturn(sides);
                        rolls.Add(roll);
                        subtotal += roll;
                    }

                    int signed = sign * subtotal;
                    total += signed;
                    details.Add($"{(sign < 0 ? "-" : "+")}{count}d{sides}({string.Join(",", rolls)})={signed}");
                }
                else
                {
                    int value = int.Parse(normalized) * sign;
                    total += value;
                    details.Add($"{(value >= 0 ? "+" : "")}{value}");
                }
            }

            if (total < 0)
            {
                details.Add("Підсумок був від'ємний, зведено до 0.");
                total = 0;
            }

            damage = total;
            breakdown = string.Join(" ", details).TrimStart('+');
            return true;
        }

        private static string NormalizeToken(string token)
            => token.Replace(" ", string.Empty);
    }
}

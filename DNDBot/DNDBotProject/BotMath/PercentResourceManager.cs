using System;
using System.Globalization;

namespace DNDBot.BotMath
{
    public static class PercentResourceManager
    {
        public static int ParseAmountSpec(string spec, int currentValue, int maxValue = -1)
        {
            if (string.IsNullOrWhiteSpace(spec))
                return 0;

            spec = spec.Trim().ToLowerInvariant();

            if (spec == "all" || spec == "max")
                return Math.Max(0, maxValue > -1 ? maxValue : currentValue);

            if (spec.EndsWith("%"))
            {
                var numPart = spec[..^1];
                if (double.TryParse(numPart, NumberStyles.Number, CultureInfo.InvariantCulture, out double perc))
                {
                    perc = Math.Clamp(perc, 0.0, 100.0);
                    return (int)Math.Ceiling(currentValue * perc / 100.0);
                }
                return 0;
            }

            if (int.TryParse(spec, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                return Math.Max(0, val);

            return 0;
        }

        public static bool TryConsumeMana(long userId, int charId, string amountSpec)
        {
            int current = ManaData.DataBaseValueReader<int>(userId, charId, "CurrentMana");
            int max = ManaData.DataBaseValueReader<int>(userId, charId, "MaxMana");
            int amount = ParseAmountSpec(amountSpec, current, max);

            if (amount <= 0)
                return false;

            if (current < amount)
                return false;

            ManaData.UpdateMagicStat(userId, charId, "CurrentMana", current - amount);
            return true;
        }

        public static int ModifyCurrentManaPercent(long userId, int charId, double percent, bool increase)
        {
            int current = ManaData.DataBaseValueReader<int>(userId, charId, "CurrentMana");
            int delta = (int)Math.Round(current * percent / 100.0);
            int newVal = increase ? current + delta : Math.Max(0, current - delta);
            ManaData.UpdateMagicStat(userId, charId, "CurrentMana", newVal);
            return newVal;
        }

        public static int ModifyMaxManaPercent(long userId, int charId, double percent, bool increase, bool keepCurrentProportion = true)
        {
            int oldMax = ManaData.DataBaseValueReader<int>(userId, charId, "MaxMana");
            int oldCurrent = ManaData.DataBaseValueReader<int>(userId, charId, "CurrentMana");

            int delta = (int)Math.Round(oldMax * percent / 100.0);
            int newMax = increase ? oldMax + delta : Math.Max(0, oldMax - delta);

            ManaData.UpdateMagicStat(userId, charId, "MaxMana", newMax);

            if (keepCurrentProportion)
            {
                double proportion = oldMax > 0 ? (double)oldCurrent / oldMax : 0.0;
                int newCurrent = (int)Math.Round(proportion * newMax);
                ManaData.UpdateMagicStat(userId, charId, "MaxMana", newCurrent);
            }

            return newMax;
        }

        public static int ModifyBaseManaPercent(long userId, int charId, double percent, bool increase)
        {
            int currentBase = ManaData.DataBaseValueReader<int>(userId, charId, "BaseMana");
            int delta = (int)Math.Round(currentBase * percent / 100.0);
            int newBase = increase ? currentBase + delta : Math.Max(0, currentBase - delta);
            ManaData.UpdateMagicStat(userId, charId, "BaseMana", newBase);
            return newBase;
        }



        public static int ModifyCurrentHealthPercent(long userId, int charId, double percent, bool increase)
        {
            int current = UserData.DataBaseValueReader<int>(userId, charId, "Health");
            int delta = (int)Math.Round(current * percent / 100.0);
            int newVal = increase ? current + delta : Math.Max(0, current - delta);
            UserData.UpdateDataBaseValue("Health", userId, charId, newVal);
            return newVal;
        }


        public static int ModifyMaxHealthPercent(long userId, int charId, double percent, bool increase, bool keepCurrentProportion = true)
        {
            int oldMax = UserData.DataBaseValueReader<int>(userId, charId, "MaxHP");
            int oldCurrent = UserData.DataBaseValueReader<int>(userId, charId, "Health");

            int delta = (int)Math.Round(oldMax * percent / 100.0);
            int newMax = increase ? oldMax + delta : Math.Max(0, oldMax - delta);

            UserData.UpdateDataBaseValue("MaxHP", userId, charId, newMax);

            if (keepCurrentProportion)
            {
                double proportion = oldMax > 0 ? (double)oldCurrent / oldMax : 0.0;
                int newCurrent = (int)Math.Round(proportion * newMax);
                UserData.UpdateDataBaseValue("Health", userId, charId, newCurrent);
            }

            return newMax;
        }


        public static int ModifyBaseHealthPercent(long userId, int charId, double percent, bool increase)
        {

            int currentBase;
            try
            {
                currentBase = UserData.DataBaseValueReader<int>(userId, charId, "BaseHP");
            }
            catch
            {
                currentBase = UserData.DataBaseValueReader<int>(userId, charId, "MaxHP");
            }

            int delta = (int)Math.Round(currentBase * percent / 100.0);
            int newBase = increase ? currentBase + delta : Math.Max(0, currentBase - delta);


            try
            {
                UserData.UpdateDataBaseValue("BaseHP", userId, charId, newBase);
            }
            catch
            {
                UserData.UpdateDataBaseValue("MaxHP", userId, charId, newBase);
            }

            return newBase;
        }
    }
}
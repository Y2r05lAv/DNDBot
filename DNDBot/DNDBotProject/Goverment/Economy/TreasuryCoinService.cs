using DNDBot.DataBase;
using System;
using System.Globalization;

namespace DNDBot.Goverment.Economy
{
    public static class TreasuryCoinService
    {
        private const string TreasuryCopperRemainderConfigKey = "TreasuryCopperRemainder";

        public const long CopperPerSilver = 10L;
        public const long SilverPerGold = 10L;
        public const long GoldPerPlatinum = 10L;
        public const long GoldPerStar = 1000L;

        public const long CopperPerGold = CopperPerSilver * SilverPerGold;          
        public const long CopperPerPlatinum = CopperPerGold * GoldPerPlatinum;      
        public const long CopperPerStar = CopperPerGold * GoldPerStar;              

        private static readonly NumberFormatInfo SpaceNumberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = "."
        };

        public readonly struct CoinBreakdown
        {
            public CoinBreakdown(long stars, long platinum, long gold, long silver, long copper)
            {
                Stars = stars;
                Platinum = platinum;
                Gold = gold;
                Silver = silver;
                Copper = copper;
            }

            public long Stars { get; }
            public long Platinum { get; }
            public long Gold { get; }
            public long Silver { get; }
            public long Copper { get; }
        }

        public static string FormatTreasury(long treasuryGold, int countryId)
        {
            long totalCopper = GetCountryTotalCopper(countryId, treasuryGold);
            return FormatFromTotalCopper(totalCopper);
        }

        public static string FormatGoldAmount(decimal goldAmount)
        {
            long totalCopper = GoldToCopper(goldAmount);
            return FormatFromTotalCopper(totalCopper);
        }

        public static string FormatSignedGoldAmount(decimal goldAmount)
        {
            if (goldAmount > 0m)
                return $"+{FormatGoldAmount(goldAmount)}";
            if (goldAmount < 0m)
                return $"-{FormatGoldAmount(Math.Abs(goldAmount))}";
            return FormatGoldAmount(0m);
        }

        public static long GetCountryTotalCopper(int countryId, long treasuryGold)
        {
            long remainder = GetCountryCopperRemainder(countryId);
            return checked(treasuryGold * CopperPerGold + remainder);
        }

        public static bool TryApplyGoldDelta(CountryRecord country, decimal goldDelta, out long oldTotalCopper, out long newTotalCopper)
        {
            oldTotalCopper = 0L;
            newTotalCopper = 0L;

            if (country == null)
                return false;

            long deltaCopper;
            try
            {
                deltaCopper = GoldToCopper(goldDelta);
                oldTotalCopper = GetCountryTotalCopper(country.CountryId, country.Treasury);
                newTotalCopper = checked(oldTotalCopper + deltaCopper);
            }
            catch
            {
                return false;
            }

            SetCountryTotalCopper(country.CountryId, country, newTotalCopper);
            return true;
        }

        public static CoinBreakdown BuildBreakdownFromTotalCopper(long totalCopper)
        {
            decimal remaining = Math.Abs((decimal)totalCopper);

            long stars = (long)decimal.Floor(remaining / CopperPerStar);
            remaining -= stars * CopperPerStar;

            long platinum = (long)decimal.Floor(remaining / CopperPerPlatinum);
            remaining -= platinum * CopperPerPlatinum;

            long gold = (long)decimal.Floor(remaining / CopperPerGold);
            remaining -= gold * CopperPerGold;

            long silver = (long)decimal.Floor(remaining / CopperPerSilver);
            remaining -= silver * CopperPerSilver;

            long copper = (long)remaining;

            return new CoinBreakdown(stars, platinum, gold, silver, copper);
        }

        private static long GetCountryCopperRemainder(int countryId)
        {
            if (countryId <= 0)
                return 0L;

            string countryKey = $"Country:{countryId}:{TreasuryCopperRemainderConfigKey}";
            decimal raw = EconomyConfigData.GetDecimal(countryKey, 0m);
            long rounded = (long)Math.Round(raw, MidpointRounding.AwayFromZero);
            if (rounded < 0L)
                return 0L;
            if (rounded >= CopperPerGold)
                return rounded % CopperPerGold;
            return rounded;
        }

        private static void SetCountryCopperRemainder(int countryId, long remainderCopper)
        {
            if (countryId <= 0)
                return;

            long normalized = remainderCopper;
            if (normalized < 0L)
                normalized = 0L;
            if (normalized >= CopperPerGold)
                normalized %= CopperPerGold;

            EconomyConfigData.UpsertCountryDecimal(countryId, TreasuryCopperRemainderConfigKey, normalized);
        }

        private static void SetCountryTotalCopper(int countryId, CountryRecord country, long totalCopper)
        {
            long goldPart = Math.DivRem(totalCopper, CopperPerGold, out long remainder);
            if (remainder < 0L)
            {
                goldPart -= 1L;
                remainder += CopperPerGold;
            }

            country.Treasury = goldPart;
            SetCountryCopperRemainder(countryId, remainder);
        }

        private static long GoldToCopper(decimal goldAmount)
        {
            decimal copper = decimal.Round(goldAmount * CopperPerGold, 0, MidpointRounding.AwayFromZero);
            return checked((long)copper);
        }

        private static string FormatFromTotalCopper(long totalCopper)
        {
            decimal totalGold = totalCopper / (decimal)CopperPerGold;
            string goldText = FormatCompact(totalGold, 2);
            var breakdown = BuildBreakdownFromTotalCopper(totalCopper);
            return $"{goldText} золота ({FormatInteger(breakdown.Stars)} зірк. / {FormatInteger(breakdown.Platinum)} плат. / {FormatInteger(breakdown.Gold)} зол. / {FormatInteger(breakdown.Silver)} сріб. / {FormatInteger(breakdown.Copper)} мід.)";
        }

        private static string FormatCompact(decimal value, int maxDecimals)
        {
            if (maxDecimals <= 0)
                return value.ToString("N0", SpaceNumberFormat);

            string format = "#,0." + new string('#', maxDecimals);
            return value.ToString(format, SpaceNumberFormat);
        }

        private static string FormatInteger(long value)
        {
            return value.ToString("N0", SpaceNumberFormat);
        }
    }
}

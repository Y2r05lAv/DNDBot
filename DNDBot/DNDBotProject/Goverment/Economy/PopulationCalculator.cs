using System;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class PopulationShares
    {
        public float Children { get; set; }
        public float WorkingAge { get; set; }
        public float Pensioners { get; set; }
        public float SocialProtected { get; set; }
    }

    public sealed class PopulationSnapshot
    {
        public long Total { get; set; }
        public long Children { get; set; }
        public long WorkingAge { get; set; }
        public long Pensioners { get; set; }
        public long SocialProtected { get; set; }
    }

    public sealed class PopulationLifecycleStats
    {
        public long StartChildren { get; set; }
        public long StartWorkingAge { get; set; }
        public long StartPensioners { get; set; }
        public long StartSocialProtected { get; set; }

        public long EndChildren { get; set; }
        public long EndWorkingAge { get; set; }
        public long EndPensioners { get; set; }
        public long EndSocialProtected { get; set; }
        public long EndTotal { get; set; }

        public long Births { get; set; }
        public long ChildToWorking { get; set; }
        public long WorkingToPensioners { get; set; }
        public long WorkingToSocial { get; set; }
        public long PensionersToSocial { get; set; }

        public long DeathsChildren { get; set; }
        public long DeathsWorking { get; set; }
        public long DeathsPensioners { get; set; }
        public long DeathsSocial { get; set; }
        public long GrowthReduction { get; set; }

        public long DeathsTotal
        {
            get
            {
                try
                {
                    return checked(DeathsChildren + DeathsWorking + DeathsPensioners + DeathsSocial);
                }
                catch (OverflowException)
                {
                    return long.MaxValue;
                }
            }
        }
    }

    public sealed class PopulationLifecycleResult
    {
        public PopulationSnapshot Snapshot { get; set; } = new();
        public PopulationLifecycleStats Stats { get; set; } = new();
    }

    public static class PopulationCalculator
    {
        private const string ChildrenStoredKey = "PopulationChildrenStored";
        private const string WorkingStoredKey = "PopulationWorkingStored";
        private const string PensionersStoredKey = "PopulationPensionersStored";
        private const string SocialStoredKey = "PopulationSocialStored";
        private const string GrowthRemainderKey = "PopulationGrowthRemainderStored";

        private const float DefaultChildrenShare = 0.25f;
        private const float DefaultWorkingShare = 0.55f;
        private const float DefaultPensionerShare = 0.15f;
        private const float DefaultSocialProtectedShare = 0.05f;

        private const float DefaultChildToWorkingRate = 0.000152f;        
        private const float DefaultWorkingToPensionRate = 0.000058f;      
        private const float DefaultWorkingToSocialRate = 0.000014f;
        private const float DefaultPensionToSocialRate = 0.00011f;

        private const float DefaultChildDeathRate = 0.000010f;
        private const float DefaultWorkingDeathRate = 0.000008f;
        private const float DefaultPensionDeathRate = 0.000140f;
        private const float DefaultSocialDeathRate = 0.000180f;

        private const float DefaultMortalityDeltaScale = 8f;

        public static PopulationShares GetShares()
        {
            float children = EconomyConfigData.GetFloat("PopulationChildrenShare", float.NaN);
            if (float.IsNaN(children))
                children = EconomyConfigData.GetFloat("DemandPopulationChildrenShare", DefaultChildrenShare);

            float working = EconomyConfigData.GetFloat("PopulationWorkingShare", float.NaN);
            if (float.IsNaN(working))
                working = EconomyConfigData.GetFloat("DemandPopulationWorkingShare", DefaultWorkingShare);

            float pensioners = EconomyConfigData.GetFloat("PopulationPensionerShare", float.NaN);
            if (float.IsNaN(pensioners))
                pensioners = EconomyConfigData.GetFloat("DemandPopulationElderlyShare", DefaultPensionerShare);

            float social = EconomyConfigData.GetFloat("PopulationSocialProtectedShare", float.NaN);
            if (float.IsNaN(social))
                social = DefaultSocialProtectedShare;

            NormalizeShares(ref children, ref working, ref pensioners, ref social);
            return new PopulationShares
            {
                Children = children,
                WorkingAge = working,
                Pensioners = pensioners,
                SocialProtected = social
            };
        }

        public static PopulationSnapshot ComputePopulation(long totalPopulation)
        {
            var shares = GetShares();
            long total = Math.Max(0L, totalPopulation);

            long children = (long)Math.Round(total * shares.Children);
            long pensioners = (long)Math.Round(total * shares.Pensioners);
            long social = (long)Math.Round(total * shares.SocialProtected);
            long working = total - children - pensioners - social;
            if (working < 0)
                working = 0;

            return new PopulationSnapshot
            {
                Total = total,
                Children = children,
                WorkingAge = working,
                Pensioners = pensioners,
                SocialProtected = social
            };
        }

        public static PopulationSnapshot GetPopulationSnapshot(CountryRecord country)
        {
            if (country == null)
                return new PopulationSnapshot();

            long total = Math.Max(0L, country.Population);
            if (country.CountryId <= 0)
                return ComputePopulation(total);

            if (!TryLoadStoredPopulation(country.CountryId, out var stored))
            {
                stored = ComputePopulation(total);
                SavePopulationSnapshot(country.CountryId, stored);
                return stored;
            }

            if (stored.Total != total)
            {
                stored = ScaleToTotal(stored, total);
                SavePopulationSnapshot(country.CountryId, stored);
            }

            if (IsExtremeDistribution(stored))
            {
                stored = SoftNormalizeToTargetShares(stored, total, ComputeNormalizationBlend(stored));
                SavePopulationSnapshot(country.CountryId, stored);
            }

            return stored;
        }

        public static void SavePopulationSnapshot(int countryId, PopulationSnapshot snapshot)
        {
            if (countryId <= 0 || snapshot == null)
                return;

            EconomyConfigData.UpsertCountryDecimal(countryId, ChildrenStoredKey, Math.Max(0m, snapshot.Children));
            EconomyConfigData.UpsertCountryDecimal(countryId, WorkingStoredKey, Math.Max(0m, snapshot.WorkingAge));
            EconomyConfigData.UpsertCountryDecimal(countryId, PensionersStoredKey, Math.Max(0m, snapshot.Pensioners));
            EconomyConfigData.UpsertCountryDecimal(countryId, SocialStoredKey, Math.Max(0m, snapshot.SocialProtected));
        }

        public static PopulationLifecycleResult AdvanceLifecycle(CountryRecord country, float growthRatePercent, float mortalityRateDelta)
        {
            if (country == null)
                return new PopulationLifecycleResult();

            var current = GetPopulationSnapshot(country);
            var stats = new PopulationLifecycleStats
            {
                StartChildren = current.Children,
                StartWorkingAge = current.WorkingAge,
                StartPensioners = current.Pensioners,
                StartSocialProtected = current.SocialProtected
            };
            decimal children = Math.Max(0m, current.Children);
            decimal working = Math.Max(0m, current.WorkingAge);
            decimal pensioners = Math.Max(0m, current.Pensioners);
            decimal social = Math.Max(0m, current.SocialProtected);
            decimal startTotal = children + working + pensioners + social;

            if (startTotal <= 0m && country.Population > 0)
            {
                var seeded = ComputePopulation(country.Population);
                children = seeded.Children;
                working = seeded.WorkingAge;
                pensioners = seeded.Pensioners;
                social = seeded.SocialProtected;
                startTotal = children + working + pensioners + social;
            }

            decimal childToWorkingRate = ClampRate(EconomyConfigData.GetFloat("LifecycleChildToWorkingRate", DefaultChildToWorkingRate));
            decimal workingToPensionRate = ClampRate(EconomyConfigData.GetFloat("LifecycleWorkingToPensionRate", DefaultWorkingToPensionRate));
            decimal workingToSocialRate = ClampRate(EconomyConfigData.GetFloat("LifecycleWorkingToSocialRate", DefaultWorkingToSocialRate));
            decimal pensionToSocialRate = ClampRate(EconomyConfigData.GetFloat("LifecyclePensionToSocialRate", DefaultPensionToSocialRate));

            if (startTotal > 0m)
            {
                var targetShares = GetShares();
                decimal childShare = children / startTotal;
                decimal childTargetShare = ClampRate(targetShares.Children);
                decimal childPressure = Math.Max(0m, childShare - childTargetShare);
                decimal childPressureBoost = Math.Max(0m, (decimal)EconomyConfigData.GetFloat("LifecycleChildPressureAgingBoost", 0.22f));
                childToWorkingRate = ClampRateDecimal(childToWorkingRate + childPressure * childPressureBoost);
            }

            decimal childToWorking = children * childToWorkingRate;
            decimal workingToPension = working * workingToPensionRate;
            decimal workingToSocial = working * workingToSocialRate;
            decimal pensionToSocial = pensioners * pensionToSocialRate;
            stats.ChildToWorking = SafeRoundToLong(childToWorking);
            stats.WorkingToPensioners = SafeRoundToLong(workingToPension);
            stats.WorkingToSocial = SafeRoundToLong(workingToSocial);
            stats.PensionersToSocial = SafeRoundToLong(pensionToSocial);

            children = Math.Max(0m, children - childToWorking);
            working = Math.Max(0m, working + childToWorking - workingToPension - workingToSocial);
            pensioners = Math.Max(0m, pensioners + workingToPension - pensionToSocial);
            social = Math.Max(0m, social + workingToSocial + pensionToSocial);

            decimal mortalityMultiplier = 1m
                + Math.Max(0m, (decimal)mortalityRateDelta)
                * (decimal)ClampMin(EconomyConfigData.GetFloat("LifecycleMortalityDeltaScale", DefaultMortalityDeltaScale), 0f);
            decimal childDeathRate = ClampRate(EconomyConfigData.GetFloat("LifecycleDeathRateChildren", DefaultChildDeathRate)) * mortalityMultiplier;
            decimal workingDeathRate = ClampRate(EconomyConfigData.GetFloat("LifecycleDeathRateWorking", DefaultWorkingDeathRate)) * mortalityMultiplier;
            decimal pensionDeathRate = ClampRate(EconomyConfigData.GetFloat("LifecycleDeathRatePensioners", DefaultPensionDeathRate)) * mortalityMultiplier;
            decimal socialDeathRate = ClampRate(EconomyConfigData.GetFloat("LifecycleDeathRateSocial", DefaultSocialDeathRate)) * mortalityMultiplier;
            decimal childDeaths = children * childDeathRate;
            decimal workingDeaths = working * workingDeathRate;
            decimal pensionDeaths = pensioners * pensionDeathRate;
            decimal socialDeaths = social * socialDeathRate;
            stats.DeathsChildren = SafeRoundToLong(childDeaths);
            stats.DeathsWorking = SafeRoundToLong(workingDeaths);
            stats.DeathsPensioners = SafeRoundToLong(pensionDeaths);
            stats.DeathsSocial = SafeRoundToLong(socialDeaths);

            children = Math.Max(0m, children - childDeaths);
            working = Math.Max(0m, working - workingDeaths);
            pensioners = Math.Max(0m, pensioners - pensionDeaths);
            social = Math.Max(0m, social - socialDeaths);

            float clampedGrowth = growthRatePercent < -50f ? -50f : growthRatePercent;
            decimal growthRemainder = country.CountryId > 0
                ? EconomyConfigData.GetDecimalForCountry(country.CountryId, GrowthRemainderKey, 0m)
                : 0m;
            if (growthRemainder < -1m || growthRemainder > 1m)
                growthRemainder = 0m;
            decimal targetTotalDec = Math.Max(0m, startTotal * (1m + (decimal)clampedGrowth / 100m) + growthRemainder);
            long targetTotal = SafeRoundToLong(targetTotalDec);
            decimal nextGrowthRemainder = targetTotalDec - targetTotal;
            if (country.CountryId > 0)
                EconomyConfigData.UpsertCountryDecimal(country.CountryId, GrowthRemainderKey, nextGrowthRemainder);

            decimal survivorsTotal = children + working + pensioners + social;
            if (targetTotal > SafeRoundToLong(survivorsTotal))
            {
                decimal births = targetTotal - survivorsTotal;
                stats.Births = SafeRoundToLong(births);
                children += births;
            }
            else if (targetTotal < SafeRoundToLong(survivorsTotal))
            {
                decimal reduction = survivorsTotal - targetTotal;
                stats.GrowthReduction = SafeRoundToLong(reduction);
                ApplyWeightedReduction(ref children, ref working, ref pensioners, ref social, reduction);
            }

            var next = BuildSnapshotFromParts(children, working, pensioners, social, targetTotal);
            stats.EndChildren = next.Children;
            stats.EndWorkingAge = next.WorkingAge;
            stats.EndPensioners = next.Pensioners;
            stats.EndSocialProtected = next.SocialProtected;
            stats.EndTotal = next.Total;
            country.Population = next.Total;
            if (country.CountryId > 0)
                SavePopulationSnapshot(country.CountryId, next);

            return new PopulationLifecycleResult
            {
                Snapshot = next,
                Stats = stats
            };
        }

        public static long ComputeCivilianWorkforce(long workingAge, int armySize, bool warState)
        {
            long pool = workingAge - Math.Max(0, armySize);
            if (pool < 0)
                pool = 0;

            if (warState)
            {
                float reduction = EconomyConfigData.GetFloat("WarWorkforceReduction", 0.1f);
                pool = SafeMulToLong(pool, 1f - EconomyCalculator.Clamp(reduction, 0f, 0.5f));
            }

            return pool;
        }

        public static float ComputeWorkRate(long population, long workingAge)
        {
            if (population <= 0)
                return 0f;
            float rate = workingAge * 100f / population;
            return EconomyCalculator.Clamp(rate, 0f, 100f);
        }

        public static float ComputeMobilizationPenalty(long workingAge, int armySize)
        {
            float factor = EconomyConfigData.GetFloat("MobilizationUnemploymentPenalty", 0f);
            if (factor <= 0f || workingAge <= 0 || armySize <= 0)
                return 0f;
            float share = EconomyCalculator.Clamp(armySize / (float)workingAge, 0f, 1f);
            return share * factor;
        }

        private static void NormalizeShares(ref float children, ref float working, ref float pensioners, ref float social)
        {
            float sum = children + working + pensioners + social;
            if (sum <= 0f)
            {
                children = DefaultChildrenShare;
                working = DefaultWorkingShare;
                pensioners = DefaultPensionerShare;
                social = DefaultSocialProtectedShare;
                return;
            }
            children /= sum;
            working /= sum;
            pensioners /= sum;
            social /= sum;
        }

        private static long SafeMulToLong(long value, float factor)
        {
            if (value <= 0 || factor <= 0f)
                return 0L;
            double result = value * (double)factor;
            if (result >= long.MaxValue) return long.MaxValue;
            if (result <= long.MinValue) return long.MinValue;
            return (long)Math.Round(result);
        }

        private static PopulationSnapshot BuildSnapshotFromParts(decimal children, decimal working, decimal pensioners, decimal social, long targetTotal)
        {
            targetTotal = Math.Max(0L, targetTotal);
            if (targetTotal <= 0L)
                return new PopulationSnapshot();

            decimal c = Math.Max(0m, children);
            decimal w = Math.Max(0m, working);
            decimal p = Math.Max(0m, pensioners);
            decimal s = Math.Max(0m, social);
            decimal sum = c + w + p + s;
            if (sum <= 0m)
                return ComputePopulation(targetTotal);

            decimal[] parts = { c, w, p, s };
            long[] rounded = new long[4];
            decimal[] fractions = new decimal[4];
            long assigned = 0L;

            for (int i = 0; i < 4; i++)
            {
                decimal scaled = parts[i] / sum * targetTotal;
                if (scaled < 0m)
                    scaled = 0m;
                long baseValue = (long)Math.Floor(scaled);
                rounded[i] = baseValue;
                fractions[i] = scaled - baseValue;
                assigned += baseValue;
            }

            long remaining = targetTotal - assigned;
            while (remaining > 0)
            {
                int pick = 0;
                decimal best = fractions[0];
                for (int i = 1; i < 4; i++)
                {
                    if (fractions[i] > best)
                    {
                        best = fractions[i];
                        pick = i;
                    }
                }
                rounded[pick] += 1;
                fractions[pick] = 0m;
                remaining--;
            }

            return new PopulationSnapshot
            {
                Children = rounded[0],
                WorkingAge = rounded[1],
                Pensioners = rounded[2],
                SocialProtected = rounded[3],
                Total = targetTotal
            };
        }

        private static PopulationSnapshot ScaleToTotal(PopulationSnapshot snapshot, long targetTotal)
        {
            if (snapshot == null)
                return ComputePopulation(Math.Max(0L, targetTotal));

            targetTotal = Math.Max(0L, targetTotal);
            if (targetTotal == 0)
                return new PopulationSnapshot();

            decimal sum = Math.Max(0m, snapshot.Children) + Math.Max(0m, snapshot.WorkingAge) + Math.Max(0m, snapshot.Pensioners) + Math.Max(0m, snapshot.SocialProtected);
            if (sum <= 0m)
                return ComputePopulation(targetTotal);

            return BuildSnapshotFromParts(snapshot.Children, snapshot.WorkingAge, snapshot.Pensioners, snapshot.SocialProtected, targetTotal);
        }

        private static bool IsExtremeDistribution(PopulationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Total <= 0)
                return false;

            decimal total = snapshot.Total;
            decimal childrenShare = snapshot.Children / total;
            decimal workingShare = snapshot.WorkingAge / total;
            decimal pensionShare = snapshot.Pensioners / total;
            decimal socialShare = snapshot.SocialProtected / total;

            return childrenShare > 0.70m
                || workingShare < 0.10m
                || pensionShare > 0.45m
                || socialShare > 0.45m;
        }

        private static decimal ComputeNormalizationBlend(PopulationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Total <= 0)
                return 0.65m;

            decimal total = snapshot.Total;
            decimal childrenShare = snapshot.Children / total;
            decimal workingShare = snapshot.WorkingAge / total;
            decimal pensionShare = snapshot.Pensioners / total;
            decimal socialShare = snapshot.SocialProtected / total;

            decimal severity = 0m;
            severity += Math.Max(0m, childrenShare - 0.55m) * 2.4m;
            severity += Math.Max(0m, 0.35m - workingShare) * 2.8m;
            severity += Math.Max(0m, pensionShare - 0.30m) * 1.5m;
            severity += Math.Max(0m, socialShare - 0.25m) * 1.5m;

            decimal blend = 0.65m + Math.Min(1m, severity) * 0.30m;
            if (childrenShare > 0.90m || workingShare < 0.05m)
                blend = Math.Max(blend, 0.93m);

            if (blend < 0.60m)
                blend = 0.60m;
            if (blend > 0.95m)
                blend = 0.95m;
            return blend;
        }

        private static PopulationSnapshot SoftNormalizeToTargetShares(PopulationSnapshot snapshot, long targetTotal, decimal blendToTarget)
        {
            targetTotal = Math.Max(0L, targetTotal);
            if (targetTotal <= 0)
                return new PopulationSnapshot();

            decimal blend = blendToTarget;
            if (blend < 0m) blend = 0m;
            if (blend > 1m) blend = 1m;

            var target = ComputePopulation(targetTotal);
            decimal keep = 1m - blend;

            decimal children = snapshot.Children * keep + target.Children * blend;
            decimal working = snapshot.WorkingAge * keep + target.WorkingAge * blend;
            decimal pensioners = snapshot.Pensioners * keep + target.Pensioners * blend;
            decimal social = snapshot.SocialProtected * keep + target.SocialProtected * blend;
            return BuildSnapshotFromParts(children, working, pensioners, social, targetTotal);
        }

        private static bool TryLoadStoredPopulation(int countryId, out PopulationSnapshot snapshot)
        {
            snapshot = new PopulationSnapshot();
            if (countryId <= 0)
                return false;

            decimal children = EconomyConfigData.GetDecimalForCountry(countryId, ChildrenStoredKey, -1m);
            decimal working = EconomyConfigData.GetDecimalForCountry(countryId, WorkingStoredKey, -1m);
            decimal pensioners = EconomyConfigData.GetDecimalForCountry(countryId, PensionersStoredKey, -1m);
            decimal social = EconomyConfigData.GetDecimalForCountry(countryId, SocialStoredKey, -1m);

            if (children < 0m || working < 0m || pensioners < 0m || social < 0m)
                return false;

            long c = SafeRoundToLong(children);
            long w = SafeRoundToLong(working);
            long p = SafeRoundToLong(pensioners);
            long s = SafeRoundToLong(social);

            snapshot = new PopulationSnapshot
            {
                Children = c,
                WorkingAge = w,
                Pensioners = p,
                SocialProtected = s,
                Total = SafeAddSafe(c, w, p, s)
            };
            return true;
        }

        private static void ApplyWeightedReduction(ref decimal children, ref decimal working, ref decimal pensioners, ref decimal social, decimal reduction)
        {
            if (reduction <= 0m)
                return;

            decimal c = Math.Max(0m, children);
            decimal w = Math.Max(0m, working);
            decimal p = Math.Max(0m, pensioners);
            decimal s = Math.Max(0m, social);
            decimal total = c + w + p + s;
            if (total <= 0m)
            {
                children = 0m;
                working = 0m;
                pensioners = 0m;
                social = 0m;
                return;
            }

            decimal[] values = { c, w, p, s };
            decimal[] weights = { 0.75m, 0.90m, 1.20m, 1.35m };
            decimal weightedTotal = 0m;
            for (int i = 0; i < 4; i++)
                weightedTotal += values[i] * weights[i];
            if (weightedTotal <= 0m)
                weightedTotal = total;

            decimal remaining = Math.Min(reduction, total);
            for (int i = 0; i < 4; i++)
            {
                decimal share = weightedTotal <= 0m ? 0m : (values[i] * weights[i]) / weightedTotal;
                decimal cut = remaining * share;
                values[i] = Math.Max(0m, values[i] - cut);
            }

            decimal after = values[0] + values[1] + values[2] + values[3];
            decimal targetAfter = Math.Max(0m, total - reduction);
            decimal leftToCut = Math.Max(0m, after - targetAfter);
            if (leftToCut > 0m)
            {
                for (int i = 3; i >= 0 && leftToCut > 0m; i--)
                {
                    decimal cut = Math.Min(values[i], leftToCut);
                    values[i] -= cut;
                    leftToCut -= cut;
                }
            }

            children = Math.Max(0m, values[0]);
            working = Math.Max(0m, values[1]);
            pensioners = Math.Max(0m, values[2]);
            social = Math.Max(0m, values[3]);
        }

        private static decimal ClampRate(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0m;
            if (value < 0f)
                return 0m;
            if (value > 1f)
                return 1m;
            return (decimal)value;
        }

        private static decimal ClampRateDecimal(decimal value)
        {
            if (value < 0m)
                return 0m;
            if (value > 1m)
                return 1m;
            return value;
        }

        private static float ClampMin(float value, float min)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return min;
            return value < min ? min : value;
        }

        private static long SafeRoundToLong(decimal value)
        {
            if (value <= 0m)
                return 0L;
            if (value >= long.MaxValue)
                return long.MaxValue;
            return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static long SafeAddSafe(long a, long b, long c, long d)
        {
            try
            {
                return checked(a + b + c + d);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }
    }
}

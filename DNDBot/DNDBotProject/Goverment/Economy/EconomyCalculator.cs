using System;

namespace DNDBot.Goverment.Economy
{
    public static class EconomyCalculator
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float ToRatio(float percent)
        {
            return percent / 100f;
        }

        public static float EmploymentRate(long availableWorkers, long totalJobs)
        {
            if (availableWorkers <= 0 || totalJobs <= 0)
                return 0f;
            return Math.Min(1f, totalJobs / (float)availableWorkers);
        }

        public static float ProductionEfficiency(float employmentRate, float stabilityPct, float corruptionPct, float warModifier, float sanctionsModifier, float festivalPenalty)
        {
            float stability = ToRatio(stabilityPct);
            float corruption = ToRatio(corruptionPct);
            float efficiency = employmentRate * stability * (1f - corruption) * warModifier * sanctionsModifier;
            efficiency *= (1f - festivalPenalty);
            return Clamp(efficiency, 0f, 2f);
        }

        public static float ApplySanctionsModifier(SanctionsLevel level, float none, float partial, float full)
        {
            return level switch
            {
                SanctionsLevel.Partial => partial,
                SanctionsLevel.Full => full,
                _ => none
            };
        }

        public static float ApplySanctionsModifier(SanctionsLevel level, float none, float partial, float full, float embargo)
        {
            return level switch
            {
                SanctionsLevel.Partial => partial,
                SanctionsLevel.Full => full,
                SanctionsLevel.Embargo => embargo,
                _ => none
            };
        }

        public static float ApplyCountrySanctionsModifier(SanctionsLevel level, float effectiveness, float none, float partial, float full)
        {
            float baseModifier = ApplySanctionsModifier(level, none, partial, full);
            float eff = Clamp(effectiveness, 0f, 1f);
            return baseModifier * eff;
        }

        public static float ApplyCountrySanctionsModifier(SanctionsLevel level, float effectiveness, float none, float partial, float full, float embargo)
        {
            float baseModifier = ApplySanctionsModifier(level, none, partial, full, embargo);
            float eff = Clamp(effectiveness, 0f, 1f);
            return baseModifier * eff;
        }

        public static float ApplyPercentModifier(float percent)
        {
            float p = Clamp(percent, 0f, 1f);
            return 1f - p;
        }

        public static long ApplyPercentModifier(long value, float penalty)
        {
            if (penalty <= 0f) return value;
            return (long)Math.Round(value * (1f - penalty));
        }
    }
}

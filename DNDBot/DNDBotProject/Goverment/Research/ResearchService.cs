using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;
using DNDBot.Goverment;

namespace DNDBot.Goverment.Research
{
    public sealed class ResearchService
    {
        public string StartResearch(int countryId, int researchId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";

            var def = ResearchDefinitions.GetById(researchId);
            if (def == null)
                return "Дослідження не знайдено.";

            if (ResearchCompletedData.IsCompleted(countryId, researchId))
                return "Це дослідження вже завершене.";

            var active = ResearchProgressData.GetActiveResearch(countryId);
            if (active != null && !active.IsCompleted)
                return "Вже є активне дослідження. Спершу завершіть його.";

            var completed = ResearchCompletedData.GetCompletedResearchIds(countryId);
            if (def.Requirements.Any(r => !completed.Contains(r)))
                return "Не виконані вимоги для цього дослідження.";

            if (country.Treasury < def.MoneyCost)
                return "Недостатньо коштів у казні.";

            country.Treasury -= def.MoneyCost;
            CountriesData.UpdateCountry(country);

            var progress = new ResearchProgressRecord
            {
                CountryId = countryId,
                ResearchId = researchId,
                Progress = 0,
                StartedAt = DateTime.Now,
                IsCompleted = false
            };
            ResearchProgressData.UpsertProgress(progress);

            return $"📘 Дослідження розпочато: {def.Name}\nВартість: {def.MoneyCost:N0} золота";
        }

        public string UpdateResearch(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";

            var active = ResearchProgressData.GetActiveResearch(countryId);
            if (active == null || active.IsCompleted)
                return "Немає активного дослідження.";

            var def = ResearchDefinitions.GetById(active.ResearchId);
            if (def == null)
                return "Дослідження не знайдено.";

            if (def.ScienceCost <= 0)
            {
                CompleteResearch(country, def, active);
                return $"✅ Дослідження завершено: {def.Name}\nЕфекти застосовано.";
            }

            float sciencePerDay = Math.Max(0, country.ResearchSpeed);
            if (sciencePerDay <= 0)
                return "Швидкість досліджень дорівнює 0.";

            float delta = (sciencePerDay / def.ScienceCost) * 100f;
            active.Progress = Math.Min(100f, active.Progress + delta);
            ResearchProgressData.UpsertProgress(active);

            if (active.Progress >= 100f)
            {
                CompleteResearch(country, def, active);
                return $"✅ Дослідження завершено: {def.Name}\nЕфекти застосовано.";
            }

            int remainingDays = EstimateRemainingDays(active.Progress, sciencePerDay, def.ScienceCost);
            return $"📘 Дослідження: {def.Name}\nПрогрес: {active.Progress:0.##}%\nНаука/день: +{sciencePerDay:0.##}\nЗалишилось: {remainingDays} дн.";
        }

        public string CompleteActiveResearchManually(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";

            var active = ResearchProgressData.GetActiveResearch(countryId);
            if (active == null || active.IsCompleted)
                return "Немає активного дослідження.";

            var def = ResearchDefinitions.GetById(active.ResearchId);
            if (def == null)
                return "Дослідження не знайдено.";

            float beforeProgress = active.Progress;
            CompleteResearch(country, def, active);
            return $"✅ Дослідження вручну завершено: {def.Name}\nПрогрес: {beforeProgress:0.##}% → 100%\nЕфекти застосовано.";
        }

        public string GetActiveResearchStatus(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";

            var active = ResearchProgressData.GetActiveResearch(countryId);
            if (active == null || active.IsCompleted)
                return "Активне дослідження відсутнє.";

            var def = ResearchDefinitions.GetById(active.ResearchId);
            if (def == null)
                return "Дослідження не знайдено.";

            float sciencePerDay = Math.Max(0, country.ResearchSpeed);
            int remainingDays = EstimateRemainingDays(active.Progress, sciencePerDay, def.ScienceCost);
            return $"📘 Дослідження: {def.Name}\nПрогрес: {active.Progress:0.##}%\nНаука/день: +{sciencePerDay:0.##}\nЗалишилось: {remainingDays} дн.";
        }

        private static int EstimateRemainingDays(float progress, float sciencePerDay, int scienceCost)
        {
            if (sciencePerDay <= 0 || scienceCost <= 0)
                return 999;
            float remainingPercent = Math.Max(0, 100f - progress);
            float perDay = (sciencePerDay / scienceCost) * 100f;
            if (perDay <= 0)
                return 999;
            return (int)Math.Ceiling(remainingPercent / perDay);
        }

        public bool CanStartResearch(int countryId, int researchId, out string reason)
        {
            reason = string.Empty;
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                reason = "Країну не знайдено.";
                return false;
            }

            var def = ResearchDefinitions.GetById(researchId);
            if (def == null)
            {
                reason = "Дослідження не знайдено.";
                return false;
            }

            if (ResearchCompletedData.IsCompleted(countryId, researchId))
            {
                reason = "Дослідження вже завершено.";
                return false;
            }

            var active = ResearchProgressData.GetActiveResearch(countryId);
            if (active != null && !active.IsCompleted)
            {
                reason = "Є активне дослідження.";
                return false;
            }

            var completed = ResearchCompletedData.GetCompletedResearchIds(countryId);
            if (def.Requirements.Any(r => !completed.Contains(r)))
            {
                reason = "Не виконані вимоги.";
                return false;
            }

            if (country.Treasury < def.MoneyCost)
            {
                reason = "Недостатньо коштів.";
                return false;
            }

            return true;
        }


        private static void CompleteResearch(CountryRecord country, ResearchDefinition def, ResearchProgressRecord progress)
        {
            bool alreadyCompleted = ResearchCompletedData.IsCompleted(country.CountryId, def.Id);

            progress.Progress = 100f;
            progress.IsCompleted = true;
            ResearchProgressData.UpsertProgress(progress);

            if (!alreadyCompleted)
            {
                ResearchCompletedData.AddCompleted(country.CountryId, def.Id);
                country.CompletedResearches += 1;
                ApplyEffects(country, def.Effects);
            }

            CountriesData.UpdateCountry(country);
            ParliamentProvisioningService.EnsureParliamentForCountry(country.CountryId);
        }

        private static void ApplyEffects(CountryRecord country, List<ResearchEffect> effects)
        {
            foreach (var effect in effects)
            {
                switch (effect.Type)
                {
                    case ResearchEffectType.EnableParliament:
                        country.HasParliament = true;
                        break;
                    case ResearchEffectType.IncreaseParliamentSeats:
                        country.ParliamentSeats += (int)effect.Amount;
                        break;
                    case ResearchEffectType.ReduceCorruption:
                        country.Corruption = Math.Max(0, country.Corruption - effect.Amount);
                        break;
                    case ResearchEffectType.IncreaseResearchSpeed:
                        country.ResearchSpeed += effect.Amount;
                        break;
                    case ResearchEffectType.Army:
                        ApplyArmyEffect(country, effect);
                        break;
                    case ResearchEffectType.Economy:
                        ApplyEconomyEffect(country, effect);
                        break;
                    case ResearchEffectType.Magic:
                        ApplyMagicEffect(country, effect);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void ApplyArmyEffect(CountryRecord country, ResearchEffect effect)
        {
            var key = NormalizeEffectKey(effect.Value);
            if (string.IsNullOrWhiteSpace(key))
                return;

            switch (key)
            {
                case "armysize":
                    country.ArmySize = ClampMin(SafeAddInt(country.ArmySize, RoundToInt(effect.Amount)), 0);
                    break;
                case "armyloyalty":
                    country.ArmyLoyalty = ClampRange(country.ArmyLoyalty + effect.Amount, 0f, 100f);
                    break;
                case "armymorale":
                    country.ArmyMorale = ClampRange(country.ArmyMorale + effect.Amount, 0f, 100f);
                    break;
                case "militaryupkeep":
                    country.MilitaryUpkeep = ClampMin(country.MilitaryUpkeep + (decimal)effect.Amount, 0m);
                    break;
                case "militaryinfluence":
                    country.MilitaryInfluence = ClampRange(country.MilitaryInfluence + effect.Amount, 0f, 100f);
                    break;
                case "intellevel":
                    country.IntelLevel = ClampRange(country.IntelLevel + effect.Amount, 0f, 100f);
                    break;
                case "counterintellevel":
                    country.CounterIntelLevel = ClampRange(country.CounterIntelLevel + effect.Amount, 0f, 100f);
                    break;
            }
        }

        private static void ApplyEconomyEffect(CountryRecord country, ResearchEffect effect)
        {
            var key = NormalizeEffectKey(effect.Value);
            if (string.IsNullOrWhiteSpace(key))
                return;

            switch (key)
            {
                case "treasury":
                    country.Treasury = SafeAddLong(country.Treasury, RoundToLong(effect.Amount));
                    break;
                case "taxlevel":
                    country.TaxLevel = ClampRange(SafeAddInt(country.TaxLevel, RoundToInt(effect.Amount)), 0, 100);
                    break;
                case "workrate":
                    country.WorkRate = ClampRange(country.WorkRate + effect.Amount, 0f, 100f);
                    break;
                case "infrastructurelevel":
                    country.InfrastructureLevel = ClampRange(country.InfrastructureLevel + effect.Amount, 0f, 100f);
                    break;
                case "logisticsefficiency":
                    country.LogisticsEfficiency = ClampRange(country.LogisticsEfficiency + effect.Amount, 0f, 100f);
                    break;
                case "inflation":
                    country.Inflation = ClampRange(country.Inflation + effect.Amount, 0f, 200f);
                    break;
                case "populationgrowth":
                    country.PopulationGrowth = ClampRange(country.PopulationGrowth + effect.Amount, -20f, 20f);
                    break;
                case "population":
                    country.Population = ClampMin(SafeAddLong(country.Population, RoundToLong(effect.Amount)), 0);
                    break;
                case "availableworkers":
                    country.AvailableWorkers = ClampMin(SafeAddLong(country.AvailableWorkers, RoundToLong(effect.Amount)), 0);
                    break;
                case "unemploymentrate":
                    country.UnemploymentRate = ClampRange(country.UnemploymentRate + effect.Amount, 0f, 100f);
                    break;
                case "incomeperturn":
                    country.IncomePerTurn = ClampMin(country.IncomePerTurn + (decimal)effect.Amount, 0m);
                    break;
                case "expensesperturn":
                    country.ExpensesPerTurn = ClampMin(country.ExpensesPerTurn + (decimal)effect.Amount, 0m);
                    break;
                case "corruptionlosses":
                    country.CorruptionLosses = ClampMin(country.CorruptionLosses + (decimal)effect.Amount, 0m);
                    break;
                case "sanctionslevel":
                    country.SanctionsLevel = ClampRange(SafeAddInt(country.SanctionsLevel, RoundToInt(effect.Amount)), 0, 3);
                    break;
                case "internationalreputation":
                    country.InternationalReputation = ClampRange(country.InternationalReputation + effect.Amount, 0f, 100f);
                    break;
            }
        }

        private static void ApplyMagicEffect(CountryRecord country, ResearchEffect effect)
        {
            var key = NormalizeEffectKey(effect.Value);
            if (string.IsNullOrWhiteSpace(key))
                return;

            switch (key)
            {
                case "magicinfluence":
                    country.MagicInfluence = ClampRange(country.MagicInfluence + effect.Amount, 0f, 100f);
                    break;
                case "manastability":
                    country.ManaStability = ClampRange(country.ManaStability + effect.Amount, 0f, 100f);
                    break;
                case "activemanazones":
                    country.ActiveManaZones = ClampMin(SafeAddInt(country.ActiveManaZones, RoundToInt(effect.Amount)), 0);
                    break;
                case "arcanerisk":
                    country.ArcaneRisk = ClampRange(country.ArcaneRisk + effect.Amount, 0f, 100f);
                    break;
                case "magesinfluence":
                    country.MagesInfluence = ClampRange(country.MagesInfluence + effect.Amount, 0f, 100f);
                    break;
            }
        }

        private static string NormalizeEffectKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static long RoundToLong(float value)
        {
            return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static int SafeAddInt(int a, int b)
        {
            long sum = (long)a + b;
            if (sum > int.MaxValue)
                return int.MaxValue;
            if (sum < int.MinValue)
                return int.MinValue;
            return (int)sum;
        }

        private static long SafeAddLong(long a, long b)
        {
            try
            {
                return checked(a + b);
            }
            catch (OverflowException)
            {
                return b >= 0 ? long.MaxValue : long.MinValue;
            }
        }

        private static int ClampRange(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float ClampRange(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static int ClampMin(int value, int min)
        {
            return value < min ? min : value;
        }

        private static long ClampMin(long value, long min)
        {
            return value < min ? min : value;
        }

        private static decimal ClampMin(decimal value, decimal min)
        {
            return value < min ? min : value;
        }
    }
}

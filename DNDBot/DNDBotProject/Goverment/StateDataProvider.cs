using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;

namespace DNDBot.Goverment
{
    public class StateDataProvider
    {
        public StateDataClass GetStateData(long userId, int? countryId = null)
        {
            if (countryId.HasValue && countryId.Value > 0)
            {
                var country = CountriesData.GetCountryById(countryId.Value);
                if (country != null)
                {
                    return MapCountryToState(country);
                }
            }

             
            return new StateDataClass
            {
                CountryName = "Республіка Авреліум",
                FormOfGovernment = "Республіка",
                HasParliament = true,

                Stability = 62,
                PopularLoyalty = 71,
                Corruption = 28,

                Treasury = 12450,
                Population = 38200L,
                PopulationGrowth = 1.4f,
                PopulationGrowthBase = 1.4f,
                PopulationGrowthStatusModifier = 0f,
                PopulationGrowthMortalityPenalty = 0f,
                PopulationGrowthFinal = 1.4f,
                WorkRate = 60,
                AvailableWorkers = 22920L,
                UnemploymentRate = 8,
                ChildrenPopulation = 9550L,
                WorkingAgePopulation = 21010L,
                PensionersPopulation = 5730L,
                SocialProtectedPopulation = 1910L,
                EmployedWorkers = 21086L,
                UnemployedWorkers = 1834L,
                CompaniesCount = 18,
                CompanyJobs = 21800L,
                CompanyAverageSalary = 1.12f,
                CompanyAverageTaxRate = 18f,
                StateOrderBudget = 450f,
                StateOrderJobsCreated = 180L,
                PensionSpending = 2006f,
                SocialProtectedSpending = 860f,
                EducationSpending = 2387f,
                UnemploymentSupportSpending = 550f,
                WarState = false,
                SanctionsLevel = 0,
                WeaponQuality = 63f,
                AmmunitionIssued = 72f,
                SupplyCoverage = 80f,
                FormationTimeMultiplier = 1.22f,
                CombatEffectiveness = 66f,
                MobilizationPressure = 12f,
                CrimeLevel = 24f,
                PoliceEffectiveness = 43f,
                DesertionRisk = 6f,
                CommanderSkill = 58f,
                CommanderLoyalty = 61f,
                CommanderCorruption = 21f,
                CommanderExperience = 49f,
                UnitStructureText = "Дивізія: 0 | Бригада: 1 | Полк: 2 | Батальйон: 3 | Рота: 6",
                IntelLevel = 0,
                CounterIntelLevel = 0
            };
        }

        private static StateDataClass MapCountryToState(CountryRecord c)
        {
            var buildings = EconomyBuildingsData.GetByCountry(c.CountryId);
            var units = MilitaryUnitsData.GetByCountry(c.CountryId);
            var obligations = VassalMilitaryObligationsData.GetByCountry(c.CountryId);
            long totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            float unemploymentRatio = EconomyCalculator.Clamp(c.UnemploymentRate / 100f, 0f, 1f);
            var mechanics = SocioMilitaryMechanics.Build(
                c,
                buildings,
                totalJobs,
                unemploymentRatio,
                c.MilitaryUpkeep,
                units,
                obligations);

            var effects = EconomyService.GetCountryEffectsPreview(c);
            var latestTurn = EconomyTurnHistoryData.GetLatestTurn(c.CountryId);
            float basePopulationGrowth = c.PopulationGrowth;
            float statusGrowthModifier = effects.PopulationGrowthBonus;
            float mortalityPenalty = latestTurn != null && latestTurn.MortalityDelta > 0f
                ? latestTurn.MortalityDelta * 100f
                : 0f;
            float finalGrowthModifier = basePopulationGrowth + statusGrowthModifier - mortalityPenalty;
            if (finalGrowthModifier < -50f)
                finalGrowthModifier = -50f;

            long availableWorkers = mechanics.CivilianWorkforce;
            if (availableWorkers <= 0 || availableWorkers > c.Population)
                availableWorkers = ComputeAvailableWorkers(c);

            return new StateDataClass
            {
                CountryName = c.Name,
                FormOfGovernment = c.FormOfGovernment,
                GovernmentLevel = c.GovernmentLevel,
                GovernmentXP = c.GovernmentXP,

                Stability = c.Stability,
                PopularLoyalty = c.PopularLoyalty,
                EliteLoyalty = c.EliteLoyalty,
                Corruption = c.Corruption,
                Legitimacy = c.Legitimacy,
                Control = c.Control,
                Inflation = c.Inflation,

                Treasury = c.Treasury,
                TaxLevel = c.TaxLevel,
                IncomePerTurn = Convert.ToSingle(c.IncomePerTurn),
                ExpensesPerTurn = Convert.ToSingle(c.ExpensesPerTurn),
                CorruptionLosses = Convert.ToSingle(c.CorruptionLosses),
                WorkRate = PopulationCalculator.ComputeWorkRate(c.Population, mechanics.Population.WorkingAge),
                AvailableWorkers = availableWorkers,
                UnemploymentRate = mechanics.UnemploymentRate * 100f,
                EmployedWorkers = mechanics.EmployedWorkers,
                UnemployedWorkers = mechanics.UnemployedWorkers,
                WarState = c.WarState,
                SanctionsLevel = c.SanctionsLevel,

                Population = c.Population,
                PopulationGrowth = c.PopulationGrowth,
                PopulationGrowthBase = basePopulationGrowth,
                PopulationGrowthStatusModifier = statusGrowthModifier,
                PopulationGrowthMortalityPenalty = mortalityPenalty,
                PopulationGrowthFinal = finalGrowthModifier,
                ChildrenPopulation = mechanics.Population.Children,
                WorkingAgePopulation = mechanics.Population.WorkingAge,
                PensionersPopulation = mechanics.Population.Pensioners,
                SocialProtectedPopulation = mechanics.Population.SocialProtected,
                Happiness = c.Happiness,
                Unrest = c.Unrest,
                Refugees = c.Refugees,
                CrimeLevel = mechanics.CrimeLevel,
                PoliceEffectiveness = mechanics.PoliceEffectiveness,

                NobilityInfluence = c.NobilityInfluence,
                MerchantsInfluence = c.MerchantsInfluence,
                ClergyInfluence = c.ClergyInfluence,
                MilitaryInfluence = c.MilitaryInfluence,
                MagesInfluence = c.MagesInfluence,
                PeasantsInfluence = c.PeasantsInfluence,
                ExternalInfluence = c.ExternalInfluence,

                HasParliament = c.HasParliament,
                ParliamentSeats = c.ParliamentSeats,
                ParliamentFractions = c.ParliamentFractions,
                ParliamentSupport = c.ParliamentSupport,
                OppositionStrength = c.OppositionStrength,

                ActiveLawsCount = c.ActiveLawsCount,
                MaxLawsAllowed = c.MaxLawsAllowed,
                EmergencyLawActive = c.EmergencyLawActive,

                ArmySize = c.ArmySize,
                ArmyLoyalty = c.ArmyLoyalty,
                ArmyMorale = c.ArmyMorale,
                MilitaryUpkeep = Convert.ToSingle(c.MilitaryUpkeep),
                MilitarySalarySpending = Convert.ToSingle(mechanics.MilitarySalarySpending),
                MilitarySupplySpending = Convert.ToSingle(mechanics.MilitarySupplySpending),
                SupplyCoverage = mechanics.SupplyCoverage * 100f,
                WeaponQuality = mechanics.WeaponQuality,
                AmmunitionIssued = mechanics.AmmunitionIssued * 100f,
                FormationTimeMultiplier = mechanics.FormationTimeMultiplier,
                CombatEffectiveness = mechanics.CombatEffectiveness,
                MobilizationPressure = mechanics.MobilizationPressure * 100f,
                DesertionRisk = mechanics.DesertionRisk * 100f,
                CommanderSkill = mechanics.CommanderSkill,
                CommanderLoyalty = mechanics.CommanderLoyalty,
                CommanderCorruption = mechanics.CommanderCorruption,
                CommanderExperience = mechanics.CommanderExperience,
                VassalRegiments = mechanics.VassalRegiments,
                VassalTroops = mechanics.VassalTroops,
                VassalFunding = Convert.ToSingle(mechanics.VassalFunding),
                UnitStructureText = FormatUnitStructure(mechanics.UnitStructure),
                MartialLaw = c.MartialLaw,

                CompaniesCount = mechanics.CompanyCount,
                CompanyJobs = mechanics.CompanyJobs,
                CompanyAverageSalary = Convert.ToSingle(mechanics.AverageSalary),
                CompanyAverageTaxRate = Convert.ToSingle(mechanics.AverageTaxRate * 100m),
                StateOrderBudget = Convert.ToSingle(mechanics.StateOrderBudget),
                StateOrderJobsCreated = mechanics.StateOrderJobsCreated,

                PensionSpending = Convert.ToSingle(mechanics.PensionSpending),
                SocialProtectedSpending = Convert.ToSingle(mechanics.SocialProtectedSpending),
                EducationSpending = Convert.ToSingle(mechanics.EducationSpending),
                UnemploymentSupportSpending = Convert.ToSingle(mechanics.UnemploymentSupportSpending),

                MagicInfluence = c.MagicInfluence,
                ManaStability = c.ManaStability,
                ActiveManaZones = c.ActiveManaZones,
                ArcaneRisk = c.ArcaneRisk,

                Cities = c.Cities,
                Villages = c.Villages,
                InfrastructureLevel = c.InfrastructureLevel,
                LogisticsEfficiency = c.LogisticsEfficiency,

                ResearchLevel = c.ResearchLevel,
                ResearchSpeed = c.ResearchSpeed,
                CompletedResearches = c.CompletedResearches,

                CoupRisk = c.CoupRisk,
                RebellionRisk = c.RebellionRisk,
                AssassinationRisk = c.AssassinationRisk,

                Allies = c.Allies,
                Enemies = c.Enemies,
                InternationalReputation = c.InternationalReputation,
                IntelLevel = c.IntelLevel,
                CounterIntelLevel = c.CounterIntelLevel,

                LastUpdate = c.LastUpdate ?? DateTime.MinValue,
                TurnsSurvived = c.TurnsSurvived,

                IsInCrisis = c.IsInCrisis,
                IsInGoldenAge = c.IsInGoldenAge,
                IsCollapsed = c.IsCollapsed
            };
        }

        private static long ComputeAvailableWorkers(CountryRecord c)
        {
            var population = PopulationCalculator.GetPopulationSnapshot(c);
            return PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, c.ArmySize, c.WarState);
        }

        private static string FormatUnitStructure(Dictionary<string, int> structure)
        {
            if (structure == null || structure.Count == 0)
                return "Немає частин";

            var ordered = new[] { "Division", "Brigade", "Regiment", "Battalion", "Company", "Platoon", "Squad" };
            var parts = new List<string>();
            foreach (var level in ordered)
            {
                if (structure.TryGetValue(level, out var count) && count > 0)
                    parts.Add($"{FormatUnitLevelUk(level)}: {count}");
            }

            if (parts.Count == 0)
                return "Немає частин";
            return string.Join(" | ", parts);
        }

        private static string FormatUnitLevelUk(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return "Відділення";

            return level.Trim().ToLowerInvariant() switch
            {
                "division" => "Дивізія",
                "brigade" => "Бригада",
                "regiment" => "Полк",
                "battalion" => "Батальйон",
                "company" => "Рота",
                "platoon" => "Взвод",
                "squad" => "Відділення",
                _ => level
            };
        }
    }
}

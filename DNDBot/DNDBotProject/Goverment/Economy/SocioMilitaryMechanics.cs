#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class SocioMilitarySnapshot
    {
        public PopulationSnapshot Population { get; set; } = new();
        public long CivilianWorkforce { get; set; }
        public long EmployedWorkers { get; set; }
        public long UnemployedWorkers { get; set; }
        public float UnemploymentRate { get; set; }

        public int CompanyCount { get; set; }
        public long CompanyJobs { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal AverageTaxRate { get; set; }
        public decimal SalaryPolicyMultiplier { get; set; } = 1m;
        public decimal Payroll { get; set; }
        public decimal StateOrderBudget { get; set; }
        public long StateOrderJobsCreated { get; set; }
        public decimal StateOrderProductionBonus { get; set; }

        public decimal PensionSpending { get; set; }
        public decimal SocialProtectedSpending { get; set; }
        public decimal EducationSpending { get; set; }
        public decimal UnemploymentSupportSpending { get; set; }
        public decimal SocialTotalSpending => PensionSpending + SocialProtectedSpending + EducationSpending + UnemploymentSupportSpending;

        public decimal MilitarySalarySpending { get; set; }
        public decimal MilitarySupplySpending { get; set; }
        public decimal MilitaryStaticUpkeep { get; set; }
        public decimal MilitaryRequiredSpending => MilitaryStaticUpkeep + MilitarySalarySpending + MilitarySupplySpending + StateOrderBudget;

        public float SupplyCoverage { get; set; }
        public float WeaponQuality { get; set; }
        public float AmmunitionIssued { get; set; }
        public float FormationTimeMultiplier { get; set; }
        public float CombatEffectiveness { get; set; }
        public float MobilizationPressure { get; set; }
        public float PoliceEffectiveness { get; set; }
        public float CrimeLevel { get; set; }
        public float DesertionRisk { get; set; }
        public bool QuickMobilization { get; set; }

        public float CommanderSkill { get; set; }
        public float CommanderLoyalty { get; set; }
        public float CommanderCorruption { get; set; }
        public float CommanderExperience { get; set; }

        public int VassalRegiments { get; set; }
        public int VassalTroops { get; set; }
        public decimal VassalFunding { get; set; }
        public Dictionary<string, int> UnitStructure { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static class SocioMilitaryMechanics
    {
        private static readonly (string Level, int Size)[] UnitStructureTemplate =
        {
            ("Division", 15000),
            ("Brigade", 5000),
            ("Regiment", 2500),
            ("Battalion", 500),
            ("Company", 120),
            ("Platoon", 30),
            ("Squad", 10)
        };

        public static SocioMilitarySnapshot Build(
            CountryRecord country,
            List<EconomyBuildingRecord> buildings,
            long totalJobs,
            float unemploymentRate,
            decimal militaryFundingAvailable,
            List<MilitaryUnitRecord>? units = null,
            List<VassalMilitaryObligationRecord>? obligations = null,
            decimal? forcedStateOrderBudget = null)
        {
            buildings ??= new List<EconomyBuildingRecord>();
            units ??= new List<MilitaryUnitRecord>();
            obligations ??= new List<VassalMilitaryObligationRecord>();
            var companyRegistry = EconomyCompaniesData.GetByCountry(country.CountryId)
                .Where(c => c.IsActive)
                .ToList();

            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long civilianWorkforce = PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);
            float unemployment = EconomyCalculator.Clamp(unemploymentRate, 0f, 1f);
            unemployment += PopulationCalculator.ComputeMobilizationPenalty(population.WorkingAge, country.ArmySize);
            unemployment = EconomyCalculator.Clamp(unemployment, 0f, 1f);

            long employedWorkers = SafeMulToLong(civilianWorkforce, 1f - unemployment);
            if (employedWorkers > civilianWorkforce)
                employedWorkers = civilianWorkforce;
            long unemployedWorkers = Math.Max(0L, civilianWorkforce - employedWorkers);

            var companyBuildings = buildings
                .Where(b => string.Equals(b.Category, "Production", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(b.Category, "Military", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int companyCount = companyRegistry.Count > 0 ? companyRegistry.Count : companyBuildings.Count;
            long companyJobs = companyRegistry.Count > 0
                ? companyRegistry.Sum(c => Math.Max(0, c.JobsOffered))
                : companyBuildings.Sum(c => Math.Max(0, c.JobsProvided));
            decimal averageSalary = companyRegistry.Count > 0
                ? ComputeAverageSalaryFromRegistry(companyRegistry, country.CountryId)
                : ComputeAverageSalary(companyBuildings, country.CountryId);
            decimal salaryPolicyMultiplier = ComputeCivilianSalaryPolicyMultiplier(country.CountryId, averageSalary);
            decimal effectiveAverageSalary = averageSalary * salaryPolicyMultiplier;
            decimal averageTaxRate = companyRegistry.Count > 0
                ? ComputeAverageTaxRateFromRegistry(companyRegistry, country.TaxLevel)
                : ComputeAverageTaxRate(companyBuildings, country.TaxLevel);
            decimal payroll = effectiveAverageSalary * employedWorkers;

            decimal stateOrderBudget = forcedStateOrderBudget ?? ComputeStateOrderBudget(country);
            decimal stateOrderJobCost = Math.Max(1m, EconomyConfigData.GetDecimal("StateOrderJobCost", 25m));
            long stateOrderJobsCreated = (long)Math.Round(stateOrderBudget / stateOrderJobCost, MidpointRounding.AwayFromZero);
            long extraEmployment = Math.Min(unemployedWorkers, Math.Max(0L, stateOrderJobsCreated));
            if (extraEmployment > 0)
            {
                employedWorkers += extraEmployment;
                unemployedWorkers = Math.Max(0L, unemployedWorkers - extraEmployment);
                unemployment = civilianWorkforce <= 0 ? 0f : unemployedWorkers / (float)civilianWorkforce;
            }
            decimal stateOrderProductionBonus = stateOrderBudget * Math.Max(0m, EconomyConfigData.GetDecimal("StateOrderProductionMultiplier", 0.45m));

            decimal pensionSpending = population.Pensioners * EconomyConfigData.GetDecimal("PensionPerPensioner", 0.35m);
            decimal socialProtectedSpending = population.SocialProtected * EconomyConfigData.GetDecimal("SocialProtectedPayment", 0.45m);
            decimal educationSpending = population.Children * EconomyConfigData.GetDecimal("EducationCostPerChild", 0.25m);
            decimal unemploymentSupportSpending = unemployedWorkers * EconomyConfigData.GetDecimal("SocialCostPerUnemployed", 0.3m);

            int ownArmy = Math.Max(0, country.ArmySize);
            decimal militarySalaryPerTurn = EconomyConfigData.GetDecimalForCountry(country.CountryId, "SoldierSalaryPerTurn", 0.6m);
            decimal militarySalarySpending = ownArmy * militarySalaryPerTurn;
            decimal militarySupplySpending = ownArmy * (
                EconomyConfigData.GetDecimal("SoldierFoodCostPerTurn", 0.25m) +
                EconomyConfigData.GetDecimal("SoldierAmmoCostPerTurn", 0.4m) +
                EconomyConfigData.GetDecimal("SoldierMedicineCostPerTurn", 0.12m));
            decimal militaryStaticUpkeep = Math.Max(0m, country.MilitaryUpkeep);

            var activeObligations = obligations.Where(o => o.IsActive).ToList();
            int vassalRegiments = activeObligations.Sum(o => Math.Max(0, o.RegimentsProvided));
            decimal vassalFunding = activeObligations.Sum(o => Math.Max(0m, o.FundingPerTurn));
            int regimentSize = Math.Max(500, EconomyConfigData.GetInt("VassalRegimentSize", 2500));
            int vassalTroops = SafeMulToInt(vassalRegiments, regimentSize);

            decimal requiredMilitarySpending = militaryStaticUpkeep + militarySalarySpending + militarySupplySpending + stateOrderBudget;
            decimal availableMilitary = Math.Max(0m, militaryFundingAvailable) + vassalFunding;
            float supplyCoverage = requiredMilitarySpending <= 0m
                ? 1f
                : EconomyCalculator.Clamp((float)(availableMilitary / requiredMilitarySpending), 0f, 1f);

            float fallbackCommanderSkill = EconomyCalculator.Clamp(35f + country.ResearchLevel * 2f + country.ArmyLoyalty * 0.3f, 0f, 100f);
            float fallbackCommanderLoyalty = EconomyCalculator.Clamp(country.ArmyLoyalty, 0f, 100f);
            float fallbackCommanderCorruption = EconomyCalculator.Clamp(country.Corruption * 0.7f, 0f, 100f);
            float fallbackCommanderExperience = EconomyCalculator.Clamp(country.TurnsSurvived * 0.2f + country.ArmyMorale * 0.35f, 0f, 100f);

            float commanderSkill = WeightedAverageUnits(units, u => u.CommanderSkill, fallbackCommanderSkill);
            float commanderLoyalty = WeightedAverageUnits(units, u => u.CommanderLoyalty, fallbackCommanderLoyalty);
            float commanderCorruption = WeightedAverageUnits(units, u => u.CommanderCorruption, fallbackCommanderCorruption);
            float commanderExperience = WeightedAverageUnits(units, u => u.CommanderExperience, fallbackCommanderExperience);

            long workingPool = Math.Max(1L, population.WorkingAge);
            float mobilizationPressure = EconomyCalculator.Clamp(ownArmy / (float)workingPool, 0f, 1f);

            float quickThreshold = EconomyConfigData.GetFloat("QuickMobilizationThreshold", 0.35f);
            bool quickMobilization = country.WarState && mobilizationPressure >= quickThreshold;

            float corruptionRatio = EconomyCalculator.Clamp(country.Corruption / 100f, 0f, 1f);
            float formationTimeMultiplier = 1f + corruptionRatio;
            if (quickMobilization)
            {
                float quickFormationMult = EconomyConfigData.GetFloat("QuickMobilizationFormationMultiplier", 0.25f);
                formationTimeMultiplier = Math.Max(0.15f, formationTimeMultiplier * EconomyCalculator.Clamp(quickFormationMult, 0.05f, 1f));
            }

            int militaryIndustryCountFromBuildings = companyBuildings.Count(c =>
                string.Equals(c.Category, "Military", StringComparison.OrdinalIgnoreCase) ||
                IsMilitaryResourceType(c.ResourceType));
            int militaryIndustryCountFromRegistry = companyRegistry.Count(c =>
                c.MilitaryProductionShare >= 25f ||
                IsMilitaryProductionType(c.ProductionType));
            int militaryIndustryCount = Math.Max(militaryIndustryCountFromBuildings, militaryIndustryCountFromRegistry);
            int buildingArmyBonus = buildings
                .Where(IsBuildingArmyBonusActive)
                .Sum(b => Math.Max(0, b.ArmyBonus));

            float weaponIndustryBonus = Math.Min(25f, militaryIndustryCount * 2.5f);
            float armyInfrastructureQualityBonus = Math.Min(30f, buildingArmyBonus * 0.3f);
            float stateOrderQualityBonus = (float)(stateOrderBudget * EconomyConfigData.GetDecimal("StateOrderWeaponQualityBonus", 0.002m));
            float manualWeaponQualityBonus = EconomyCalculator.Clamp((float)EconomyConfigData.GetDecimalForCountry(country.CountryId, "ManualWeaponQualityBonus", 0m), -100f, 100f);
            float weaponCorruptionPenalty = EconomyConfigData.GetFloat("WeaponQualityCorruptionPenalty", 0.45f);
            float weaponQuality = 50f
                + weaponIndustryBonus
                + armyInfrastructureQualityBonus
                + stateOrderQualityBonus
                + manualWeaponQualityBonus
                + commanderSkill * 0.1f
                + commanderExperience * 0.08f
                - country.Corruption * weaponCorruptionPenalty;

            float unitsWeapon = WeightedAverageUnits(units, u => u.WeaponReadiness, weaponQuality);
            weaponQuality = EconomyCalculator.Clamp((weaponQuality + unitsWeapon) * 0.5f, 5f, 100f);

            float ammoCorruptionPenalty = EconomyConfigData.GetFloat("AmmoCorruptionPenalty", 0.35f);
            float ammunitionIssued = EconomyCalculator.Clamp(supplyCoverage * (1f - corruptionRatio * ammoCorruptionPenalty), 0f, 1f);
            if (quickMobilization)
                ammunitionIssued = EconomyCalculator.Clamp(ammunitionIssued * 0.75f, 0f, 1f);

            float unitTraining = WeightedAverageUnits(units, u => u.TrainingLevel, 0f);
            float unitMorale = WeightedAverageUnits(units, u => u.Morale, country.ArmyMorale);
            float unitSupply = WeightedAverageUnits(units, u => u.SupplyLevel, supplyCoverage * 100f);
            float baseTraining = EconomyConfigData.GetFloat("TrainingBaseFromResearch", 20f)
                + country.ResearchLevel * 2f
                + commanderExperience * 0.2f
                + country.LogisticsEfficiency * 0.2f
                + Math.Min(20f, buildingArmyBonus * 0.2f);
            baseTraining -= mobilizationPressure * EconomyConfigData.GetFloat("TrainingMobilizationPenalty", 30f);
            if (quickMobilization)
                baseTraining -= EconomyConfigData.GetFloat("QuickMobilizationTrainingPenalty", 25f);

            float training = units.Count > 0
                ? (baseTraining * 0.4f + unitTraining * 0.6f)
                : baseTraining;
            training = EconomyCalculator.Clamp(training, 0f, 100f);

            float morale = units.Count > 0
                ? (country.ArmyMorale * 0.5f + unitMorale * 0.5f)
                : country.ArmyMorale;
            if (quickMobilization)
                morale -= EconomyConfigData.GetFloat("QuickMobilizationMoralePenalty", 20f);
            morale = EconomyCalculator.Clamp(morale, 0f, 100f);

            float logistics = EconomyCalculator.Clamp(country.LogisticsEfficiency / 100f, 0f, 1f);
            float commanderFactor = (commanderSkill * 0.55f + commanderExperience * 0.45f) / 100f;
            float combat = (training / 100f) * 0.23f
                + (morale / 100f) * 0.2f
                + (weaponQuality / 100f) * 0.2f
                + ammunitionIssued * 0.17f
                + logistics * 0.12f
                + commanderFactor * 0.08f;

            combat *= 0.55f + EconomyCalculator.Clamp((supplyCoverage + unitSupply / 100f) * 0.5f, 0f, 1f) * 0.45f;
            combat *= 1f - corruptionRatio * EconomyConfigData.GetFloat("CombatCorruptionPenalty", 0.25f);
            combat *= 1f + Math.Min(0.35f, Math.Max(0f, buildingArmyBonus) / 300f);
            if (quickMobilization)
                combat *= 0.82f;
            float combatEffectiveness = EconomyCalculator.Clamp(combat * 100f, 1f, 100f);

            float desertionRisk = (1f - supplyCoverage) * EconomyConfigData.GetFloat("SupplyDesertionWeight", 0.45f)
                + mobilizationPressure * EconomyConfigData.GetFloat("MobilizationDesertionWeight", 0.3f)
                + corruptionRatio * EconomyConfigData.GetFloat("CorruptionDesertionWeight", 0.2f);
            if (quickMobilization)
                desertionRisk += 0.08f;
            if (country.WarState)
                desertionRisk += (1f - morale / 100f) * 0.05f;
            desertionRisk = EconomyCalculator.Clamp(desertionRisk, 0f, 0.8f);

            float police = EconomyConfigData.GetFloat("PoliceBaseEffectiveness", 25f)
                + country.Control * EconomyConfigData.GetFloat("PoliceControlWeight", 0.35f)
                + country.IntelLevel * EconomyConfigData.GetFloat("PoliceIntelWeight", 0.2f)
                - country.Corruption * 0.2f;
            if (country.MartialLaw)
                police += 10f;
            police = EconomyCalculator.Clamp(police, 0f, 100f);

            float crime = unemployment * 100f * EconomyConfigData.GetFloat("CrimeUnemploymentWeight", 0.35f)
                + country.Corruption * EconomyConfigData.GetFloat("CrimeCorruptionWeight", 0.45f)
                + mobilizationPressure * 100f * EconomyConfigData.GetFloat("CrimeMobilizationWeight", 0.25f)
                + (1f - supplyCoverage) * 100f * EconomyConfigData.GetFloat("CrimeSupplyWeight", 0.2f);

            if (activeObligations.Count > 0)
            {
                float vassalTension = activeObligations.Sum(o =>
                    Math.Max(0f, 40f - EconomyCalculator.Clamp(o.Loyalty, 0f, 100f)) * 0.2f +
                    Math.Max(0f, o.RebellionRisk) * 0.1f);
                crime += vassalTension;
            }

            crime -= police * EconomyConfigData.GetFloat("CrimePoliceSuppression", 0.55f);
            if (country.MartialLaw)
                crime *= EconomyConfigData.GetFloat("CrimeMartialLawSuppression", 0.7f);
            crime = EconomyCalculator.Clamp(crime, 0f, 100f);

            var structure = BuildUnitStructure(ownArmy + vassalTroops, units);

            return new SocioMilitarySnapshot
            {
                Population = population,
                CivilianWorkforce = civilianWorkforce,
                EmployedWorkers = employedWorkers,
                UnemployedWorkers = unemployedWorkers,
                UnemploymentRate = unemployment,
                CompanyCount = companyCount,
                CompanyJobs = companyJobs,
                AverageSalary = effectiveAverageSalary,
                AverageTaxRate = averageTaxRate,
                SalaryPolicyMultiplier = salaryPolicyMultiplier,
                Payroll = payroll,
                StateOrderBudget = stateOrderBudget,
                StateOrderJobsCreated = stateOrderJobsCreated,
                StateOrderProductionBonus = stateOrderProductionBonus,
                PensionSpending = pensionSpending,
                SocialProtectedSpending = socialProtectedSpending,
                EducationSpending = educationSpending,
                UnemploymentSupportSpending = unemploymentSupportSpending,
                MilitarySalarySpending = militarySalarySpending,
                MilitarySupplySpending = militarySupplySpending,
                MilitaryStaticUpkeep = militaryStaticUpkeep,
                SupplyCoverage = supplyCoverage,
                WeaponQuality = weaponQuality,
                AmmunitionIssued = ammunitionIssued,
                FormationTimeMultiplier = formationTimeMultiplier,
                CombatEffectiveness = combatEffectiveness,
                MobilizationPressure = mobilizationPressure,
                PoliceEffectiveness = police,
                CrimeLevel = crime,
                DesertionRisk = desertionRisk,
                QuickMobilization = quickMobilization,
                CommanderSkill = commanderSkill,
                CommanderLoyalty = commanderLoyalty,
                CommanderCorruption = commanderCorruption,
                CommanderExperience = commanderExperience,
                VassalRegiments = vassalRegiments,
                VassalTroops = vassalTroops,
                VassalFunding = vassalFunding,
                UnitStructure = structure
            };
        }

        public static decimal ComputeStateOrderBudget(CountryRecord country)
        {
            decimal share = EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderTreasuryShare", 0.02m);
            decimal maxBudget = Math.Max(0m, EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderBudgetMax", 10000m));
            decimal budget = Math.Max(0m, country.Treasury) * Math.Max(0m, share);
            budget = Math.Min(maxBudget, budget);
            if (country.WarState)
            {
                decimal warMult = Math.Max(1m, EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderWarMultiplier", 1.35m));
                budget *= warMult;
            }
            return Math.Max(0m, budget);
        }

        private static decimal ComputeAverageSalaryFromRegistry(List<EconomyCompanyRecord> companies, int countryId)
        {
            if (companies == null || companies.Count == 0)
                return EconomyConfigData.GetDecimalForCountry(
                    countryId,
                    "CompanyBaseSalary",
                    EconomyConfigData.GetDecimalForCountry(countryId, "BaseWage", 1m));

            long totalJobs = companies.Sum(c => Math.Max(0, c.JobsOffered));
            if (totalJobs <= 0)
            {
                return companies.Average(c => Math.Max(0m, c.BaseSalary));
            }

            decimal weightedSalary = 0m;
            foreach (var company in companies)
            {
                long jobs = Math.Max(0, company.JobsOffered);
                if (jobs == 0)
                    continue;
                weightedSalary += Math.Max(0m, company.BaseSalary) * jobs;
            }

            if (weightedSalary <= 0m)
                return 0m;
            return weightedSalary / totalJobs;
        }

        private static decimal ComputeAverageTaxRateFromRegistry(List<EconomyCompanyRecord> companies, int taxLevelPercent)
        {
            if (companies == null || companies.Count == 0)
                return (decimal)EconomyCalculator.Clamp(taxLevelPercent / 100f, 0f, 1f);

            long totalJobs = companies.Sum(c => Math.Max(0, c.JobsOffered));
            if (totalJobs <= 0)
                return ClampDecimal(companies.Average(c => Math.Max(0m, c.TaxRate)), 0m, 0.95m);

            decimal weightedTax = 0m;
            foreach (var company in companies)
            {
                long jobs = Math.Max(0, company.JobsOffered);
                if (jobs == 0)
                    continue;
                weightedTax += ClampDecimal(Math.Max(0m, company.TaxRate), 0m, 0.95m) * jobs;
            }

            if (weightedTax <= 0m)
                return 0m;
            return weightedTax / totalJobs;
        }

        private static decimal ComputeAverageSalary(List<EconomyBuildingRecord> companies, int countryId)
        {
            decimal baseSalary = EconomyConfigData.GetDecimalForCountry(
                countryId,
                "CompanyBaseSalary",
                EconomyConfigData.GetDecimalForCountry(countryId, "BaseWage", 1m));
            if (companies == null || companies.Count == 0)
                return baseSalary;

            decimal privateMult = EconomyConfigData.GetDecimal("CompanyPrivateSalaryMultiplier", 1.1m);
            decimal stateMult = EconomyConfigData.GetDecimal("CompanyStateSalaryMultiplier", 0.95m);
            decimal levelStep = EconomyConfigData.GetDecimal("CompanyLevelSalaryStep", 0.03m);

            decimal sum = 0m;
            foreach (var company in companies)
            {
                decimal ownership = company.StateOwned ? stateMult : privateMult;
                decimal level = 1m + Math.Max(0, company.Level - 1) * levelStep;
                decimal militaryPremium = string.Equals(company.Category, "Military", StringComparison.OrdinalIgnoreCase) ? 1.1m : 1m;
                sum += baseSalary * ownership * level * militaryPremium;
            }

            return sum / companies.Count;
        }

        private static decimal ComputeCivilianSalaryPolicyMultiplier(int countryId, decimal currentAverageSalary)
        {
            decimal policyAverageSalary = Math.Max(
                0m,
                EconomyConfigData.GetDecimalForCountry(
                    countryId,
                    "CompanyBaseSalary",
                    EconomyConfigData.GetDecimalForCountry(countryId, "BaseWage", 1m)));

            if (currentAverageSalary <= 0m)
                return 1m;
            if (policyAverageSalary <= 0m)
                return 0m;

            decimal multiplier = policyAverageSalary / currentAverageSalary;
            return ClampDecimal(multiplier, 0m, 20m);
        }

        private static decimal ComputeAverageTaxRate(List<EconomyBuildingRecord> companies, int taxLevelPercent)
        {
            decimal baseTax = (decimal)EconomyCalculator.Clamp(taxLevelPercent / 100f, 0f, 1f);
            if (companies == null || companies.Count == 0)
                return baseTax;

            decimal privateBonus = EconomyConfigData.GetDecimal("CompanyTaxBonusPrivate", 0.04m);
            decimal stateBonus = EconomyConfigData.GetDecimal("CompanyTaxBonusState", -0.03m);

            decimal sum = 0m;
            foreach (var company in companies)
            {
                decimal bonus = company.StateOwned ? stateBonus : privateBonus;
                sum += ClampDecimal(baseTax + bonus, 0m, 0.95m);
            }

            return sum / companies.Count;
        }

        private static bool IsMilitaryResourceType(string? resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return false;

            if (MilitaryArsenalCatalog.IsMilitaryResourceType(resourceType))
                return true;

            return resourceType.Equals("Weapons", StringComparison.OrdinalIgnoreCase)
                || resourceType.Equals("Ammunition", StringComparison.OrdinalIgnoreCase)
                || resourceType.Equals("Armor", StringComparison.OrdinalIgnoreCase)
                || resourceType.Equals("Artillery", StringComparison.OrdinalIgnoreCase)
                || resourceType.Equals("Support", StringComparison.OrdinalIgnoreCase)
                || resourceType.Equals("Tools", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBuildingArmyBonusActive(EconomyBuildingRecord? building)
        {
            if (building == null || building.BuildTurns > 0)
                return false;

            return string.Equals(building.BuildingStatus, BuildingStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.BuildingStatus, BuildingStatus.Degraded.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMilitaryProductionType(string? productionType)
        {
            if (string.IsNullOrWhiteSpace(productionType))
                return false;

            string value = productionType.Trim().ToLowerInvariant();
            return value.Contains("military")
                || value.Contains("weapon")
                || value.Contains("ammo")
                || value.Contains("armor")
                || value.Contains("artillery")
                || value.Contains("defense")
                || value.Contains("siege")
                || value.Contains("arcane")
                || value.Contains("arsenal")
                || value.Contains("військ")
                || value.Contains("збро")
                || value.Contains("амун")
                || value.Contains("облог")
                || value.Contains("арк");
        }

        private static float WeightedAverageUnits(List<MilitaryUnitRecord> units, Func<MilitaryUnitRecord, float> selector, float fallback)
        {
            if (units == null || units.Count == 0)
                return fallback;

            double weightSum = 0d;
            double valueSum = 0d;
            foreach (var unit in units)
            {
                int soldiers = Math.Max(1, unit.Soldiers);
                weightSum += soldiers;
                valueSum += EconomyCalculator.Clamp(selector(unit), 0f, 100f) * soldiers;
            }

            if (weightSum <= 0d)
                return fallback;
            return EconomyCalculator.Clamp((float)(valueSum / weightSum), 0f, 100f);
        }

        private static Dictionary<string, int> BuildUnitStructure(int soldiers, List<MilitaryUnitRecord> units)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (units != null && units.Count > 0)
            {
                foreach (var grp in units.GroupBy(u => string.IsNullOrWhiteSpace(u.UnitLevel) ? "Squad" : u.UnitLevel))
                {
                    result[grp.Key] = grp.Count();
                }
                return result;
            }

            int remaining = Math.Max(0, soldiers);
            foreach (var entry in UnitStructureTemplate)
            {
                if (remaining <= 0)
                {
                    result[entry.Level] = 0;
                    continue;
                }

                int count = remaining / entry.Size;
                remaining %= entry.Size;
                result[entry.Level] = count;
            }

            if (remaining > 0)
                result["Squad"] = result.TryGetValue("Squad", out var current) ? current + 1 : 1;

            return result;
        }

        private static long SafeMulToLong(long value, float factor)
        {
            if (value <= 0 || factor <= 0f)
                return 0L;

            double result = value * factor;
            if (result >= long.MaxValue)
                return long.MaxValue;
            if (result <= long.MinValue)
                return long.MinValue;
            return (long)Math.Round(result);
        }

        private static int SafeMulToInt(int value, int factor)
        {
            long product = (long)value * factor;
            if (product > int.MaxValue)
                return int.MaxValue;
            if (product < int.MinValue)
                return int.MinValue;
            return (int)product;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DNDBot.DataBase;
using DNDBot.Goverment;

namespace DNDBot.Goverment.Economy
{
    public sealed class EconomyService
    {
        private static readonly NumberFormatInfo SpaceNumberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = "."
        };

        public sealed class CountryEffectsPreview
        {
            public float ProductionModifier { get; set; }
            public float IncomeModifier { get; set; }
            public float ExpenseModifier { get; set; }
            public float UnemploymentDelta { get; set; }
            public float MortalityRateDelta { get; set; }
            public float StabilityBonus { get; set; }
            public float PopulationGrowthBonus { get; set; }
            public float ResearchBonus { get; set; }
            public int ArmyBonus { get; set; }
        }

        private sealed class CountryEffectModifiers
        {
            public EconomyModifiers Modifiers { get; set; } = new();
            public float ProductionModifier { get; set; }
            public float IncomeModifier { get; set; }
            public float ExpenseModifier { get; set; }
            public float UnemploymentDelta { get; set; }
            public float MortalityRateDelta { get; set; }
        }

        private const string AcceptedLawStatus = "Accepted";
        private const string AddTreasuryDailyEffectType = "AddTreasuryDaily";
        private const string RemoveTreasuryDailyEffectType = "RemoveTreasuryDaily";
        private const string LimitImportEffectType = "LimitImportPercent";
        private const string LimitExportEffectType = "LimitExportPercent";
        private const string LimitCompanyResourceUseEffectType = "LimitCompanyResourceUsePercent";
        private const string SocialContributionEffectType = "SocialContributionPercent";
        private const string GrossIncomeTaxEffectType = "GrossIncomeTaxPercent";
        private const string NetIncomeTaxEffectType = "NetIncomeTaxPercent";
        private const string SocialContributionRateConfigKey = "SocialContributionRate";
        private const string AdditionalGrossIncomeTaxRateConfigKey = "AdditionalGrossIncomeTaxRate";
        private const string AdditionalNetIncomeTaxRateConfigKey = "AdditionalNetIncomeTaxRate";
        private const string ScienceLevelProgressConfigKey = "ScienceLevelProgress";
        private const string NetStabilizerEnabledConfigKey = "NetStabilizerEnabled";
        private const string NetStabilizerTargetDailyNetConfigKey = "NetStabilizerTargetDailyNet";
        private const string NetStabilizerToleranceDailyNetConfigKey = "NetStabilizerToleranceDailyNet";
        private const string NetStabilizerSinkRatioConfigKey = "NetStabilizerSinkRatio";
        private const string NetStabilizerMaxExcessSpendShareConfigKey = "NetStabilizerMaxExcessSpendShare";
        private const string ProductionAutoGrowthEnabledConfigKey = "ProductionAutoGrowthEnabled";
        private const string ProductionAutoGrowthFactorConfigKey = "ProductionAutoGrowthFactor";
        private const string SectorRevenueMultiplierConfigPrefix = "SectorRevenueMultiplier:";
        private const string SectorTaxOverrideConfigPrefix = "SectorTaxOverride:";
        private const decimal SectorTaxOverrideAutoValue = -1m;
        private const decimal SectorRevenueMultiplierMin = 0.1m;
        private const decimal SectorRevenueMultiplierMax = 5m;

        private sealed class ActiveLawEconomyPolicy
        {
            public decimal TreasuryDailyDelta { get; set; }
            public decimal ImportLimitFactor { get; set; } = 1m;
            public decimal ExportLimitFactor { get; set; } = 1m;
            public Dictionary<string, decimal> ImportLimitByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, decimal> ExportLimitByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public decimal CompanyResourceUseFactor { get; set; } = 1m;
            public decimal SocialContributionRateDelta { get; set; }
            public decimal GrossIncomeTaxRateDelta { get; set; }
            public decimal NetIncomeTaxRateDelta { get; set; }
        }

        private sealed class ParsedLawEffect
        {
            public string EffectType { get; set; } = string.Empty;
            public decimal? EffectValue { get; set; }
        }

        private sealed class CompanyResourceDemandResult
        {
            public decimal TotalRequired { get; set; }
            public decimal TotalConsumed { get; set; }
            public Dictionary<int, decimal> CoverageByCompanyId { get; } = new();
            public decimal CoveragePercent => TotalRequired <= 0m
                ? 100m
                : decimal.Round((TotalConsumed / TotalRequired) * 100m, 2, MidpointRounding.AwayFromZero);
        }

        public static CountryEffectsPreview GetCountryEffectsPreview(CountryRecord country)
        {
            var effects = BuildCountryEffects(country);
            var festivals = EconomyFestivalsData.GetActiveByCountry(country.CountryId);
            var buildings = EconomyBuildingsData.GetByCountry(country.CountryId);
            var runtimeMods = AggregateFestivalModifiers(festivals, buildings);
            var combinedMods = CombineEconomyModifiers(runtimeMods, effects.Modifiers);

            return new CountryEffectsPreview
            {
                ProductionModifier = effects.ProductionModifier - runtimeMods.ProductionPenalty,
                IncomeModifier = effects.IncomeModifier - runtimeMods.IncomePenalty,
                ExpenseModifier = effects.ExpenseModifier,
                UnemploymentDelta = effects.UnemploymentDelta,
                MortalityRateDelta = effects.MortalityRateDelta,
                StabilityBonus = combinedMods.StabilityBonus,
                PopulationGrowthBonus = combinedMods.PopulationGrowthBonus,
                ResearchBonus = combinedMods.ResearchBonus,
                ArmyBonus = combinedMods.ArmyBonus
            };
        }

        private static CountryEffectModifiers BuildCountryEffects(CountryRecord country)
        {
            var effects = new CountryEffectModifiers();

            if (country.IsCollapsed)
            {
                effects.ProductionModifier -= 0.25f;
                effects.IncomeModifier -= 0.25f;
                effects.ExpenseModifier += 0.15f;
                effects.UnemploymentDelta += 0.08f;
                effects.MortalityRateDelta += 0.01f;
                effects.Modifiers.StabilityBonus -= 15f;
                effects.Modifiers.PopulationGrowthBonus -= 0.03f;
                return effects;
            }

            if (country.IsInCrisis)
            {
                effects.ProductionModifier -= 0.10f;
                effects.IncomeModifier -= 0.10f;
                effects.ExpenseModifier += 0.05f;
                effects.UnemploymentDelta += 0.03f;
                effects.MortalityRateDelta += 0.002f;
                effects.Modifiers.StabilityBonus -= 5f;
                effects.Modifiers.PopulationGrowthBonus -= 0.01f;
            }
            else if (country.IsInGoldenAge)
            {
                effects.ProductionModifier += 0.10f;
                effects.IncomeModifier += 0.10f;
                effects.ExpenseModifier -= 0.05f;
                effects.UnemploymentDelta -= 0.02f;
                effects.MortalityRateDelta -= 0.001f;
                effects.Modifiers.StabilityBonus += 5f;
                effects.Modifiers.PopulationGrowthBonus += 0.01f;
            }

            return effects;
        }

        private static EconomyModifiers CombineEconomyModifiers(EconomyModifiers baseMods, EconomyModifiers extraMods)
        {
            return new EconomyModifiers
            {
                ProductionPenalty = EconomyCalculator.Clamp(baseMods.ProductionPenalty + extraMods.ProductionPenalty, 0f, 0.9f),
                IncomePenalty = EconomyCalculator.Clamp(baseMods.IncomePenalty + extraMods.IncomePenalty, 0f, 0.9f),
                StabilityBonus = baseMods.StabilityBonus + extraMods.StabilityBonus,
                PopulationGrowthBonus = baseMods.PopulationGrowthBonus + extraMods.PopulationGrowthBonus,
                CorruptionReduction = baseMods.CorruptionReduction + extraMods.CorruptionReduction,
                FestivalBoost = baseMods.FestivalBoost + extraMods.FestivalBoost,
                ResearchBonus = baseMods.ResearchBonus + extraMods.ResearchBonus,
                ArmyBonus = baseMods.ArmyBonus + extraMods.ArmyBonus
            };
        }

        private static string FmtNumber(decimal value, int decimals = 0)
        {
            return value.ToString($"N{decimals}", SpaceNumberFormat);
        }

        private static string FmtNumber(long value)
        {
            return value.ToString("N0", SpaceNumberFormat);
        }

        private static string FmtNumberCompact(decimal value, int maxDecimals = 2)
        {
            if (maxDecimals <= 0)
                return value.ToString("N0", SpaceNumberFormat);
            string format = "#,0." + new string('#', maxDecimals);
            return value.ToString(format, SpaceNumberFormat);
        }

        private static string FmtNumberCompact(float value, int maxDecimals = 2)
        {
            return FmtNumberCompact((decimal)value, maxDecimals);
        }

        private static string FmtNumberCompact(int value)
        {
            return FmtNumber(value);
        }

        private static string FmtNumberCompact(long value)
        {
            return FmtNumber(value);
        }

        private static string FmtNumberCompact(double value, int maxDecimals = 2)
        {
            return FmtNumberCompact((decimal)value, maxDecimals);
        }

        public static decimal GetInflationPriceMultiplier(float inflationPercent)
        {
            float sensitivity = Math.Max(0f, EconomyConfigData.GetFloat("InflationPriceSensitivity", 1f));
            decimal multiplier = 1m + ((decimal)inflationPercent / 100m) * (decimal)sensitivity;
            return ClampDecimal(multiplier, 0.05m, 20m);
        }

        public EconomyTurnResult ApplyTurn(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                return new EconomyTurnResult { Summary = "Країну не знайдено." };
            }

            var context = LoadContext(countryId);
            var computation = ComputeTurn(country, context, previewDays: null, persistBuildings: true);

            country.Treasury += computation.NetResult;
            country.TurnsSurvived += 1;
            country.LastUpdate = DateTime.Now;
            country.IsInCrisis = CheckCrisis(country, computation.UnemploymentRate);
            ApplyScienceProgression(country, computation.ScienceSpending, computation.ResearchBonus);

            CountriesData.UpdateCountry(country);
            PersistPublicSystemsContext(context);
            decimal blackMarketPurchased = SumBlackMarketPurchased(computation.Market);
            decimal blackMarketValue = SumBlackMarketValue(computation.Market);

            EconomyTurnHistoryData.AddTurn(
                country.CountryId,
                country.TurnsSurvived,
                computation.Income,
                computation.Expenses,
                computation.NetResult,
                computation.CorruptionLoss,
                computation.EmploymentRate * 100f,
                computation.UnemploymentRate * 100f,
                computation.ProductionValue,
                computation.TradeIncome,
                computation.WagesTax + computation.ProfitTax + computation.SocialContributionIncome + computation.AdditionalGrossIncomeTax + computation.AdditionalNetIncomeTax,
                country.Stability,
                computation.Market.GetRequested(ResourceType.Food),
                computation.Market.GetRequested(ResourceType.Clothing),
                computation.Market.GetRequested(ResourceType.Housing),
                computation.Market.GetRequested(ResourceType.Tools),
                computation.Market.GetRequested(ResourceType.Luxury),
                blackMarketPurchased,
                blackMarketValue,
                computation.ShortageReport.TotalShortageScore,
                computation.ShortageReport.Impact.HappinessDelta,
                computation.ShortageReport.Impact.MortalityRateDelta,
                computation.ShortageReport.Impact.ProductivityDelta,
                computation.PopulationBirths,
                computation.PopulationChildToWorking,
                computation.PopulationWorkingToPensioners,
                computation.PopulationWorkingToSocial,
                computation.PopulationPensionersToSocial,
                computation.PopulationDeathsChildren,
                computation.PopulationDeathsWorking,
                computation.PopulationDeathsPensioners,
                computation.PopulationDeathsSocial,
                computation.PopulationGrowthReduction,
                computation.PopulationStartChildren,
                computation.PopulationStartWorkingAge,
                computation.PopulationStartPensioners,
                computation.PopulationStartSocialProtected,
                computation.PopulationEndChildren,
                computation.PopulationEndWorkingAge,
                computation.PopulationEndPensioners,
                computation.PopulationEndSocialProtected
            );

            LogTurnMoney(
                country.CountryId,
                country.TurnsSurvived,
                computation.WagesTax,
                computation.ProfitTax,
                computation.SocialContributionIncome,
                computation.AdditionalGrossIncomeTax,
                computation.AdditionalNetIncomeTax,
                computation.TradeIncome,
                computation.Upkeep,
                computation.GovSalaries,
                computation.SocialSpending,
                computation.PensionSpending,
                computation.SocialProtectedSpending,
                computation.EducationSpending,
                computation.UnemploymentSupportSpending,
                computation.ScienceSpending,
                computation.FestivalCost,
                computation.MilitaryUpkeep,
                computation.MilitarySalarySpending,
                computation.MilitarySupplySpending,
                computation.StateOrderBudget,
                computation.InfrastructureSpending,
                computation.StabilizationReserveFunding,
                computation.CorruptionLoss,
                computation.DeputySalaryTotal,
                computation.DeputyTheft,
                computation.AgreementExportIncome,
                computation.AgreementImportCost,
                computation.VassalFunding,
                computation.StateCompanyDividends,
                computation.LawTreasuryDailyIncome,
                computation.LawTreasuryDailyExpense
            );

            UpdateFestivals(context.Festivals);
            string summary = BuildSummary(country, computation, computation.DeficitShortfall);

            string trancheSummary = new CurrencyExchangeService().ProcessAidTranchesForCountry(countryId);
            if (!string.IsNullOrWhiteSpace(trancheSummary))
                summary = string.IsNullOrWhiteSpace(summary) ? trancheSummary : $"{summary}\n{trancheSummary}";

            return new EconomyTurnResult
            {
                TurnNumber = country.TurnsSurvived,
                Income = (long)Math.Round(computation.Income),
                Expenses = (long)Math.Round(computation.Expenses),
                NetResult = computation.NetResult,
                CorruptionLoss = (float)computation.CorruptionLoss,
                EmploymentRate = computation.EmploymentRate * 100f,
                UnemploymentRate = computation.UnemploymentRate * 100f,
                ProductionEfficiency = computation.ProductionEfficiency * 100f,
                ShortageScore = computation.ShortageReport.TotalShortageScore,
                HappinessDelta = computation.ShortageReport.Impact.HappinessDelta,
                MortalityDelta = computation.ShortageReport.Impact.MortalityRateDelta,
                ProductivityDelta = computation.ShortageReport.Impact.ProductivityDelta,
                WorkingAgePopulation = computation.WorkingAgePopulation,
                EmployedWorkers = computation.EmployedWorkers,
                UnemployedWorkers = computation.UnemployedWorkers,
                StateOrderBudget = computation.StateOrderBudget,
                WeaponQuality = computation.WeaponQuality,
                AmmunitionIssued = computation.AmmunitionIssued * 100f,
                SupplyCoverage = computation.SupplyCoverage * 100f,
                FormationTimeMultiplier = computation.FormationTimeMultiplier,
                CombatEffectiveness = computation.CombatEffectiveness,
                CrimeLevel = computation.CrimeLevel,
                CrisisTriggered = country.IsInCrisis,
                Summary = summary
            };
        }

        public EconomyPreviewResult PreviewTurn(int countryId, int days)
        {
            var country = CountriesData.GetCountryById(countryId);
            int previewDays = Math.Max(1, days);
            if (country == null)
            {
                return new EconomyPreviewResult
                {
                    Days = previewDays,
                    Warnings = new List<EconomyWarning>
                    {
                        new EconomyWarning
                        {
                            WarningType = EconomyWarningType.ExternalShock,
                            Severity = EconomyWarningSeverity.Warning,
                            Source = EconomyWarningSource.External,
                            Message = "Країну не знайдено."
                        }
                    }
                };
            }

            var previewCountry = CloneCountry(country);
            var context = LoadContext(countryId);
            decimal totalIncome = 0m;
            decimal totalExpenses = 0m;
            long totalNet = 0L;
            long totalDeficitShortfall = 0L;

            decimal totalWagesTax = 0m;
            decimal totalProfitTax = 0m;
            decimal totalSocialContributionIncome = 0m;
            decimal totalAdditionalGrossIncomeTax = 0m;
            decimal totalAdditionalNetIncomeTax = 0m;
            decimal totalTradeIncome = 0m;
            decimal totalAgreementExportIncome = 0m;
            decimal totalAgreementImportCost = 0m;
            decimal totalStateCompanyDividends = 0m;
            decimal totalLawTreasuryDailyIncome = 0m;
            decimal totalLawTreasuryDailyExpense = 0m;
            decimal totalCitizenGrossIncome = 0m;
            decimal totalCitizenNetIncome = 0m;
            decimal totalUpkeep = 0m;
            decimal totalGovSalaries = 0m;
            decimal totalSocialSpending = 0m;
            decimal totalScienceSpending = 0m;
            decimal totalFestivalCost = 0m;
            decimal totalMilitaryUpkeep = 0m;
            decimal totalMilitarySalarySpending = 0m;
            decimal totalMilitarySupplySpending = 0m;
            decimal totalStateOrderBudget = 0m;
            decimal totalInfrastructureSpending = 0m;
            decimal totalStabilizationReserveFunding = 0m;
            decimal totalCorruptionLoss = 0m;
            decimal totalDeputySalary = 0m;
            decimal totalDeputyTheft = 0m;
            decimal totalVassalFunding = 0m;
            decimal totalBudgetInfrastructure = 0m;
            decimal totalBudgetMilitary = 0m;
            decimal totalBudgetScience = 0m;
            decimal totalBudgetSocial = 0m;
            decimal totalCompanyResourceRequired = 0m;
            decimal totalCompanyResourceConsumed = 0m;

            EconomyComputation lastDayComputation = new();

            for (int i = 0; i < previewDays; i++)
            {
                 
                var dayComputation = ComputeTurn(previewCountry, context, previewDays: null, persistBuildings: false);
                lastDayComputation = dayComputation;

                totalIncome += dayComputation.Income;
                totalExpenses += dayComputation.Expenses;
                totalNet += dayComputation.NetResult;
                totalDeficitShortfall += dayComputation.DeficitShortfall;

                totalWagesTax += dayComputation.WagesTax;
                totalProfitTax += dayComputation.ProfitTax;
                totalSocialContributionIncome += dayComputation.SocialContributionIncome;
                totalAdditionalGrossIncomeTax += dayComputation.AdditionalGrossIncomeTax;
                totalAdditionalNetIncomeTax += dayComputation.AdditionalNetIncomeTax;
                totalTradeIncome += dayComputation.TradeIncome;
                totalAgreementExportIncome += dayComputation.AgreementExportIncome;
                totalAgreementImportCost += dayComputation.AgreementImportCost;
                totalStateCompanyDividends += dayComputation.StateCompanyDividends;
                totalLawTreasuryDailyIncome += dayComputation.LawTreasuryDailyIncome;
                totalLawTreasuryDailyExpense += dayComputation.LawTreasuryDailyExpense;
                totalCitizenGrossIncome += dayComputation.CitizenGrossIncome;
                totalCitizenNetIncome += dayComputation.CitizenNetIncome;
                totalUpkeep += dayComputation.Upkeep;
                totalGovSalaries += dayComputation.GovSalaries;
                totalSocialSpending += dayComputation.SocialSpending;
                totalScienceSpending += dayComputation.ScienceSpending;
                totalFestivalCost += dayComputation.FestivalCost;
                totalMilitaryUpkeep += dayComputation.MilitaryUpkeep;
                totalMilitarySalarySpending += dayComputation.MilitarySalarySpending;
                totalMilitarySupplySpending += dayComputation.MilitarySupplySpending;
                totalStateOrderBudget += dayComputation.StateOrderBudget;
                totalInfrastructureSpending += dayComputation.InfrastructureSpending;
                totalStabilizationReserveFunding += dayComputation.StabilizationReserveFunding;
                totalCorruptionLoss += dayComputation.CorruptionLoss;
                totalDeputySalary += dayComputation.DeputySalaryTotal;
                totalDeputyTheft += dayComputation.DeputyTheft;
                totalVassalFunding += dayComputation.VassalFunding;
                totalBudgetInfrastructure += dayComputation.BudgetInfrastructure;
                totalBudgetMilitary += dayComputation.BudgetMilitary;
                totalBudgetScience += dayComputation.BudgetScience;
                totalBudgetSocial += dayComputation.BudgetSocial;
                totalCompanyResourceRequired += dayComputation.CompanyResourceRequired;
                totalCompanyResourceConsumed += dayComputation.CompanyResourceConsumed;

                previewCountry.Treasury += dayComputation.NetResult;
            }

            var warnings = BuildWarnings(countryId, previewCountry, lastDayComputation, context);
            int risk = ComputeCrisisRisk(previewCountry, lastDayComputation.UnemploymentRate);

            return new EconomyPreviewResult
            {
                Days = previewDays,
                TreasuryNow = country.Treasury,
                TreasuryAfter = previewCountry.Treasury,
                Income = (long)Math.Round(totalIncome),
                Expenses = (long)Math.Round(totalExpenses),
                NetResult = totalNet,
                DeficitShortfall = totalDeficitShortfall,
                WagesTax = totalWagesTax,
                ProfitTax = totalProfitTax,
                SocialContributionIncome = totalSocialContributionIncome,
                AdditionalGrossIncomeTax = totalAdditionalGrossIncomeTax,
                AdditionalNetIncomeTax = totalAdditionalNetIncomeTax,
                TradeIncome = totalTradeIncome,
                AgreementExportIncome = totalAgreementExportIncome,
                AgreementImportCost = totalAgreementImportCost,
                StateCompanyDividends = totalStateCompanyDividends,
                LawTreasuryDailyIncome = totalLawTreasuryDailyIncome,
                LawTreasuryDailyExpense = totalLawTreasuryDailyExpense,
                CitizenGrossIncome = totalCitizenGrossIncome,
                CitizenNetIncome = totalCitizenNetIncome,
                MainIncomeTaxRate = lastDayComputation.MainIncomeTaxRate,
                SocialContributionRate = lastDayComputation.SocialContributionRate,
                AdditionalGrossIncomeTaxRate = lastDayComputation.AdditionalGrossIncomeTaxRate,
                AdditionalNetIncomeTaxRate = lastDayComputation.AdditionalNetIncomeTaxRate,
                Upkeep = totalUpkeep,
                GovSalaries = totalGovSalaries,
                SocialSpending = totalSocialSpending,
                ScienceSpending = totalScienceSpending,
                FestivalCost = totalFestivalCost,
                MilitaryUpkeep = totalMilitaryUpkeep,
                MilitarySalarySpending = totalMilitarySalarySpending,
                MilitarySupplySpending = totalMilitarySupplySpending,
                StateOrderBudget = totalStateOrderBudget,
                InfrastructureSpending = totalInfrastructureSpending,
                StabilizationReserveFunding = totalStabilizationReserveFunding,
                CorruptionLoss = totalCorruptionLoss,
                DeputySalaryTotal = totalDeputySalary,
                DeputyTheft = totalDeputyTheft,
                VassalFunding = totalVassalFunding,
                BudgetInfrastructure = totalBudgetInfrastructure,
                BudgetMilitary = totalBudgetMilitary,
                BudgetScience = totalBudgetScience,
                BudgetSocial = totalBudgetSocial,
                ImportLimitFactor = lastDayComputation.ImportLimitFactor,
                ExportLimitFactor = lastDayComputation.ExportLimitFactor,
                CompanyResourceUseFactor = lastDayComputation.CompanyResourceUseFactor,
                CompanyResourceRequired = totalCompanyResourceRequired,
                CompanyResourceConsumed = totalCompanyResourceConsumed,
                CompanyResourceCoverage = totalCompanyResourceRequired <= 0m
                    ? 100m
                    : decimal.Round((totalCompanyResourceConsumed / totalCompanyResourceRequired) * 100m, 2, MidpointRounding.AwayFromZero),
                StabilityNow = country.Stability,
                StabilityAfter = previewCountry.Stability,
                UnemploymentNow = country.UnemploymentRate,
                UnemploymentAfter = lastDayComputation.UnemploymentRate * 100f,
                CrisisRisk = risk,
                Warnings = warnings
            };
        }

        private sealed class EconomyContext
        {
            public List<EconomyBuildingRecord> Buildings { get; set; } = new();
            public List<EconomyCompanyRecord> Companies { get; set; } = new();
            public Dictionary<string, EconomyResourceRecord> Resources { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public List<EconomyFestivalRecord> Festivals { get; set; } = new();
            public List<EconomyTradeAgreementRecord> TradeAgreements { get; set; } = new();
            public List<EconomyGlobalModifierRecord> GlobalModifiers { get; set; } = new();
            public List<MilitaryUnitRecord> MilitaryUnits { get; set; } = new();
            public List<VassalMilitaryObligationRecord> VassalObligations { get; set; } = new();
            public ActiveLawEconomyPolicy LawPolicy { get; set; } = new();
            public CountryPublicSystemsData.CountryPublicSystemsRecord PublicSystems { get; set; } = new();
            public List<CountryDiseasesData.CountryDiseaseRecord> Diseases { get; set; } = new();
            public List<CountryIntlProgramsData.CountryIntlProgramRecord> IntlPrograms { get; set; } = new();
            public List<LawRecord> AcceptedLaws { get; set; } = new();
        }

        private sealed class EconomyComputation
        {
            public decimal Income { get; set; }
            public decimal Expenses { get; set; }
            public decimal ProductionValue { get; set; }
            public decimal TradeIncome { get; set; }
            public decimal WagesTax { get; set; }
            public decimal ProfitTax { get; set; }
            public decimal SocialContributionIncome { get; set; }
            public decimal AdditionalGrossIncomeTax { get; set; }
            public decimal AdditionalNetIncomeTax { get; set; }
            public decimal AgreementExportIncome { get; set; }
            public decimal AgreementImportCost { get; set; }
            public decimal CitizenGrossIncome { get; set; }
            public decimal CitizenNetIncome { get; set; }
            public decimal MainIncomeTaxRate { get; set; }
            public decimal SocialContributionRate { get; set; }
            public decimal AdditionalGrossIncomeTaxRate { get; set; }
            public decimal AdditionalNetIncomeTaxRate { get; set; }
            public decimal Upkeep { get; set; }
            public decimal GovSalaries { get; set; }
            public decimal SocialSpending { get; set; }
            public decimal PensionSpending { get; set; }
            public decimal SocialProtectedSpending { get; set; }
            public decimal EducationSpending { get; set; }
            public decimal UnemploymentSupportSpending { get; set; }
            public decimal ScienceSpending { get; set; }
            public float ResearchBonus { get; set; }
            public decimal FestivalCost { get; set; }
            public decimal MilitaryUpkeep { get; set; }
            public decimal MilitarySalarySpending { get; set; }
            public decimal MilitarySupplySpending { get; set; }
            public decimal StateOrderBudget { get; set; }
            public decimal InfrastructureSpending { get; set; }
            public decimal CorruptionLoss { get; set; }
            public decimal DeputySalaryTotal { get; set; }
            public decimal DeputyTheft { get; set; }
            public decimal VassalFunding { get; set; }
            public decimal StabilizationReserveFunding { get; set; }
            public decimal StateCompanyDividends { get; set; }
            public decimal LawTreasuryDailyIncome { get; set; }
            public decimal LawTreasuryDailyExpense { get; set; }
            public decimal CompanyPayroll { get; set; }
            public decimal CompanyRevenue { get; set; }
            public decimal CompanyResourceRequired { get; set; }
            public decimal CompanyResourceConsumed { get; set; }
            public decimal CompanyResourceCoverage { get; set; } = 100m;
            public decimal ImportLimitFactor { get; set; } = 1m;
            public decimal ExportLimitFactor { get; set; } = 1m;
            public decimal CompanyResourceUseFactor { get; set; } = 1m;
            public decimal BudgetInfrastructure { get; set; }
            public decimal BudgetMilitary { get; set; }
            public decimal BudgetScience { get; set; }
            public decimal BudgetSocial { get; set; }
            public decimal PublicHealthBudgetSpent { get; set; }
            public decimal PublicEducationBudgetSpent { get; set; }
            public decimal PublicIntlBudgetSpent { get; set; }
            public decimal PublicEmergencyHealthSpend { get; set; }
            public decimal PublicEmergencyEducationSpend { get; set; }
            public decimal PublicTotalBudgetSpent { get; set; }
            public decimal PublicBudgetDebt { get; set; }
            public decimal PublicBudgetShortfall { get; set; }
            public float PublicProductionMultiplier { get; set; } = 1f;
            public float PublicHealthNeed { get; set; }
            public int PublicActiveDiseases { get; set; }
            public float PublicHealthPublic { get; set; }
            public float PublicHealthSanitation { get; set; }
            public float PublicHealthCapacity { get; set; }
            public float PublicHealthTrust { get; set; }
            public float PublicEduLiteracy { get; set; }
            public float PublicEduQuality { get; set; }
            public int PublicEduSpecialists { get; set; }
            public float PublicEduCohesion { get; set; }
            public float PublicIntlPrestige { get; set; }
            public string PublicTickNotes { get; set; } = string.Empty;
            public float EmploymentRate { get; set; }
            public float UnemploymentRate { get; set; }
            public float ProductionEfficiency { get; set; }
            public long WorkingAgePopulation { get; set; }
            public long EmployedWorkers { get; set; }
            public long UnemployedWorkers { get; set; }
            public long ChildrenPopulation { get; set; }
            public long PensionersPopulation { get; set; }
            public long SocialProtectedPopulation { get; set; }
            public long PopulationBirths { get; set; }
            public long PopulationChildToWorking { get; set; }
            public long PopulationWorkingToPensioners { get; set; }
            public long PopulationWorkingToSocial { get; set; }
            public long PopulationPensionersToSocial { get; set; }
            public long PopulationDeathsChildren { get; set; }
            public long PopulationDeathsWorking { get; set; }
            public long PopulationDeathsPensioners { get; set; }
            public long PopulationDeathsSocial { get; set; }
            public long PopulationGrowthReduction { get; set; }
            public long PopulationStartChildren { get; set; }
            public long PopulationStartWorkingAge { get; set; }
            public long PopulationStartPensioners { get; set; }
            public long PopulationStartSocialProtected { get; set; }
            public long PopulationEndChildren { get; set; }
            public long PopulationEndWorkingAge { get; set; }
            public long PopulationEndPensioners { get; set; }
            public long PopulationEndSocialProtected { get; set; }
            public float WeaponQuality { get; set; }
            public float AmmunitionIssued { get; set; }
            public float SupplyCoverage { get; set; }
            public float FormationTimeMultiplier { get; set; }
            public float CombatEffectiveness { get; set; }
            public int ArmyBonus { get; set; }
            public float CrimeLevel { get; set; }
            public float PoliceEffectiveness { get; set; }
            public float MobilizationPressure { get; set; }
            public float DesertionRisk { get; set; }
            public int VassalRegiments { get; set; }
            public int VassalTroops { get; set; }
            public Dictionary<string, int> UnitStructure { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public ResourceMarketResult Market { get; set; } = new();
            public ShortageReport ShortageReport { get; set; } = new();
            public long NetResult { get; set; }
            public long DeficitShortfall { get; set; }
            public SectorDistribution SectorDistribution { get; set; } = new();
        }

        private sealed class TradeAgreementEffects
        {
            public decimal ExportIncome { get; set; }
            public decimal ImportCost { get; set; }
            public Dictionary<string, decimal> ImportByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, decimal> ExportByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private EconomyContext LoadContext(int countryId)
        {
            return new EconomyContext
            {
                Buildings = EconomyBuildingsData.GetByCountry(countryId),
                Companies = EconomyCompaniesData.GetByCountry(countryId),
                Resources = EconomyResourcesData.GetAll().ToDictionary(r => r.ResourceType, StringComparer.OrdinalIgnoreCase),
                Festivals = EconomyFestivalsData.GetActiveByCountry(countryId),
                TradeAgreements = EconomyTradeAgreementsData.GetActiveByCountry(countryId),
                GlobalModifiers = EconomyGlobalModifiersData.GetActive(),
                MilitaryUnits = MilitaryUnitsData.GetByCountry(countryId),
                VassalObligations = VassalMilitaryObligationsData.GetByCountry(countryId),
                LawPolicy = BuildActiveLawEconomyPolicy(countryId),
                PublicSystems = CountryPublicSystemsData.GetByCountry(countryId),
                Diseases = CountryDiseasesData.GetByCountry(countryId),
                IntlPrograms = CountryIntlProgramsData.GetByCountry(countryId),
                AcceptedLaws = LawsData.GetByCountryAndStatus(countryId, AcceptedLawStatus)
            };
        }

        private EconomyComputation ComputeTurn(CountryRecord country, EconomyContext context, int? previewDays, bool persistBuildings)
        {
            var buildings = context.Buildings;
            var companies = context.Companies;
            var resources = context.Resources;
            var festivals = context.Festivals;
            var tradeAgreements = context.TradeAgreements;
            int simulationDays = Math.Max(1, previewDays ?? 1);
            var lawPolicy = context.LawPolicy ?? new ActiveLawEconomyPolicy();
            decimal inflationPriceMultiplier = GetInflationPriceMultiplier(country.Inflation);

            var publicTick = PublicSystemsEngine.ApplyTick(
                country,
                context.PublicSystems,
                context.Diseases,
                context.IntlPrograms,
                context.AcceptedLaws,
                allowRandomEvents: persistBuildings);

            var countryEffects = BuildCountryEffects(country);
            var diplomacy = DiplomacyManager.GetCountryModifiers(country.CountryId);
            var populationSnapshot = PopulationCalculator.GetPopulationSnapshot(country);
            long availableWorkers = PopulationCalculator.ComputeCivilianWorkforce(populationSnapshot.WorkingAge, country.ArmySize, country.WarState);

            var lifecycle = UpdateBuildingLifecycle(buildings, availableWorkers, country, persistBuildings);
            var activeBuildings = buildings.Where(lifecycle.IsBuildingActive).ToList();
            long totalJobs = lifecycle.TotalJobs;
            float employmentRate = EconomyCalculator.EmploymentRate(availableWorkers, totalJobs);
            float unemploymentRate = 1f - employmentRate;

            var festivalMods = AggregateFestivalModifiers(festivals, activeBuildings);
            var combinedMods = CombineEconomyModifiers(festivalMods, countryEffects.Modifiers);
            float warModifier = country.WarState ? EconomyConfigData.GetFloat("WarProductionModifier", 0.85f) : 1f;
            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)country.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f)
            );
            float warTradeModifier = country.WarState ? EconomyConfigData.GetFloat("WarTradeModifier", 0.7f) : 1f;
            var partnerSanctions = EconomyCountrySanctionsData.GetBySource(country.CountryId)
                .ToDictionary(s => s.TargetCountryId, s => s);
            var productSanctions = EconomyCountryProductSanctionsData.GetBySource(country.CountryId);
            var productSanctionsMap = productSanctions.ToDictionary(
                s => $"{s.TargetCountryId}|{s.ResourceType}".ToLowerInvariant(),
                s => s);
            var tradeEffects = ComputeTradeAgreementEffects(
                tradeAgreements,
                resources,
                partnerSanctions,
                productSanctionsMap,
                sanctionsModifier,
                warTradeModifier,
                inflationPriceMultiplier,
                previewDays,
                lawPolicy.ImportLimitFactor,
                lawPolicy.ExportLimitFactor,
                lawPolicy.ImportLimitByResource,
                lawPolicy.ExportLimitByResource);

            float globalProductionMod = SumGlobalModifier(context.GlobalModifiers, m => m.ProductionModifier);
            float globalTradeMod = SumGlobalModifier(context.GlobalModifiers, m => m.TradeModifier);
            float globalIncomeMod = SumGlobalModifier(context.GlobalModifiers, m => m.IncomeModifier);
            float globalStabilityMod = SumGlobalModifier(context.GlobalModifiers, m => m.StabilityModifier);
            float globalUnemploymentMod = SumGlobalModifier(context.GlobalModifiers, m => m.UnemploymentModifier);
            float unemploymentRateAdjusted = EconomyCalculator.Clamp(unemploymentRate + globalUnemploymentMod + countryEffects.UnemploymentDelta + diplomacy.UnemploymentDelta, 0f, 1f);

            var baseMechanics = SocioMilitaryMechanics.Build(
                country,
                activeBuildings,
                totalJobs,
                unemploymentRateAdjusted,
                country.MilitaryUpkeep,
                context.MilitaryUnits,
                context.VassalObligations);

            availableWorkers = baseMechanics.CivilianWorkforce;
            employmentRate = availableWorkers <= 0 ? 0f : baseMechanics.EmployedWorkers / (float)availableWorkers;
            unemploymentRateAdjusted = baseMechanics.UnemploymentRate;
            var sectorEffects = ComputeSectorImbalanceEffects(lifecycle.SectorDistribution);

            float productionEfficiency = EconomyCalculator.ProductionEfficiency(
                employmentRate,
                country.Stability,
                country.Corruption,
                warModifier,
                sanctionsModifier,
                combinedMods.ProductionPenalty
            );
            float companyBudgetModifier = ComputeCompanyBudgetPressureModifier(companies, simulationDays, country.CountryId);
            productionEfficiency = EconomyCalculator.Clamp(
                productionEfficiency * (1f + globalProductionMod + sectorEffects.ProductionModifier + countryEffects.ProductionModifier + diplomacy.ProductionModifier) * companyBudgetModifier,
                0.05f,
                2f
            );
            productionEfficiency = EconomyCalculator.Clamp(
                productionEfficiency * Math.Max(0.1f, publicTick.ProductionMultiplier),
                0.05f,
                2f);

            var demandConfig = BuildDemandConfig();
            var impactConfig = BuildDemandImpactConfig();
            var demandInput = BuildDemandInput(
                country,
                festivalMods,
                buildings,
                context.GlobalModifiers,
                resources,
                diplomacy.HappinessDelta,
                diplomacy.BlackMarketRiskDelta,
                inflationPriceMultiplier);
            var importFlow = ConvertResourceFlowDictionary(tradeEffects.ImportByResource);
            var exportFlow = ConvertResourceFlowDictionary(tradeEffects.ExportByResource);
            var preShortagePlans = BuildProductionPlans(buildings, lifecycle, productionEfficiency, resources);
            var preShortageFlow = BuildingResourceFlowCalculator.Calculate(preShortagePlans, importFlow, exportFlow);
            var companyResourceDemand = ApplyCompanyResourceDemand(
                companies,
                preShortageFlow.AvailableByResource,
                simulationDays,
                lawPolicy.CompanyResourceUseFactor);
            var supplySnapshot = BuildSupplySnapshot(preShortageFlow.AvailableByResource, resources);
            var market = new ResourceDemandService().CalculateMarket(demandInput, supplySnapshot, demandConfig);
            if (IsCountryInWealthyState(country))
                CoverResourceShortagesForWealthyState(market);
            var shortageReport = new ShortageEvaluator().EvaluateShortages(market, impactConfig);

            country.Happiness = EconomyCalculator.Clamp(country.Happiness + shortageReport.Impact.HappinessDelta + diplomacy.HappinessDelta, 0f, 100f);
            float productivityGrowthCap = Math.Max(0f, EconomyConfigData.GetFloat("ProductivityGrowthCapPerTurn", 0.02f));
            float productivityDropCap = Math.Max(0f, EconomyConfigData.GetFloat("ProductivityDropCapPerTurn", 0.06f));
            float boundedProductivityDelta = EconomyCalculator.Clamp(
                shortageReport.Impact.ProductivityDelta,
                -productivityDropCap,
                productivityGrowthCap);
            productionEfficiency = EconomyCalculator.Clamp(productionEfficiency * (1f + boundedProductivityDelta), 0.05f, 2f);

            var postShortagePlans = BuildProductionPlans(buildings, lifecycle, productionEfficiency, resources);
            var postShortageFlow = BuildingResourceFlowCalculator.Calculate(postShortagePlans, importFlow);
            decimal productionValue = 0m;
            foreach (var pair in postShortageFlow.ProducedByResource)
            {
                var basePrice = resources.TryGetValue(pair.Key, out var res) ? res.BasePrice : 1m;
                var price = basePrice * inflationPriceMultiplier;
                productionValue += pair.Value * price;
            }
            productionValue += baseMechanics.StateOrderProductionBonus;

            float taxRate = EconomyCalculator.Clamp(country.TaxLevel / 100f, 0f, 1f);
            float taxEfficiency = EconomyConfigData.GetFloat("TaxEfficiencyBase", 0.6f);
            decimal mainIncomeTaxRate = ClampDecimal((decimal)(taxRate * taxEfficiency), 0m, 0.95m);
            decimal socialContributionRate = ClampDecimal(
                NormalizeRateFactor(EconomyConfigData.GetDecimalForCountry(
                    country.CountryId,
                    SocialContributionRateConfigKey,
                    EconomyConfigData.GetDecimal(SocialContributionRateConfigKey, 0m))) + lawPolicy.SocialContributionRateDelta,
                0m,
                0.95m);
            decimal additionalGrossIncomeTaxRate = ClampDecimal(
                NormalizeRateFactor(EconomyConfigData.GetDecimalForCountry(
                    country.CountryId,
                    AdditionalGrossIncomeTaxRateConfigKey,
                    EconomyConfigData.GetDecimal(AdditionalGrossIncomeTaxRateConfigKey, 0m))) + lawPolicy.GrossIncomeTaxRateDelta,
                0m,
                0.95m);
            decimal additionalNetIncomeTaxRate = ClampDecimal(
                NormalizeRateFactor(EconomyConfigData.GetDecimalForCountry(
                    country.CountryId,
                    AdditionalNetIncomeTaxRateConfigKey,
                    EconomyConfigData.GetDecimal(AdditionalNetIncomeTaxRateConfigKey, 0m))) + lawPolicy.NetIncomeTaxRateDelta,
                0m,
                0.95m);

            decimal wagesTax = 0m;
            decimal profitTax = productionValue * baseMechanics.AverageTaxRate * (decimal)taxEfficiency * EconomyConfigData.GetDecimal("ProfitTaxFactor", 0.6m);
            decimal stateCompanyDividends = 0m;
            decimal companyPayroll = baseMechanics.Payroll;
            decimal companyRevenue = 0m;

            decimal exportShare = EconomyConfigData.GetDecimal("ExportShare", 0.3m);
            float tradeBonus = tradeAgreements.Sum(a => Math.Max(0f, a.ExportBonus));
            decimal tradeIncome = productionValue * exportShare * (decimal)(1f + tradeBonus) * (decimal)sanctionsModifier * (decimal)warTradeModifier;
            decimal agreementExportIncome = tradeEffects.ExportIncome;
            decimal agreementImportCost = tradeEffects.ImportCost;

            tradeIncome += agreementExportIncome - agreementImportCost;
            tradeIncome *= (decimal)(1f + globalTradeMod + diplomacy.TradeModifier);

            var companyTurn = ProcessCompanyEconomyTurn(
                country,
                companies,
                baseMechanics,
                productionValue,
                tradeIncome,
                taxEfficiency,
                baseMechanics.SalaryPolicyMultiplier,
                simulationDays,
                persistBuildings,
                companyResourceDemand.CoverageByCompanyId);

            if (companyTurn.HasCompanies)
            {
                companyPayroll = companyTurn.Payroll;
                companyRevenue = companyTurn.Revenue;
                profitTax = companyTurn.ProfitTax;
                stateCompanyDividends = companyTurn.StateDividends;
            }

            decimal citizenGrossIncome = Math.Max(0m, companyPayroll);
            wagesTax = citizenGrossIncome * mainIncomeTaxRate;
            decimal socialContributionIncome = citizenGrossIncome * socialContributionRate;
            decimal additionalGrossIncomeTax = citizenGrossIncome * additionalGrossIncomeTaxRate;
            decimal netIncomeTaxBase = Math.Max(0m, citizenGrossIncome - wagesTax - socialContributionIncome - additionalGrossIncomeTax);
            decimal additionalNetIncomeTax = netIncomeTaxBase * additionalNetIncomeTaxRate;
            decimal citizenNetIncome = Math.Max(0m, netIncomeTaxBase - additionalNetIncomeTax);

            decimal lawTreasuryDailyIncome = Math.Max(0m, lawPolicy.TreasuryDailyDelta) * simulationDays;
            decimal lawTreasuryDailyExpense = Math.Max(0m, -lawPolicy.TreasuryDailyDelta) * simulationDays;
            decimal income = wagesTax + profitTax + socialContributionIncome + additionalGrossIncomeTax + additionalNetIncomeTax + tradeIncome + stateCompanyDividends + lawTreasuryDailyIncome;
            income = EconomyCalculator.ApplyPercentModifier((long)Math.Round(income), combinedMods.IncomePenalty);
            income *= (decimal)(1f + globalIncomeMod + countryEffects.IncomeModifier + diplomacy.IncomeModifier);

            int deputyCount = 0;
            float avgDeputyCorruption = 0f;
            decimal deputySalaryBase = EconomyConfigData.GetDecimalForCountry(country.CountryId, "DeputySalaryBase", 5m);
            decimal deputySalaryTotal = 0m;
            decimal deputyTheft = 0m;
            if (country.HasParliament)
            {
                var deputies = DeputiesData.GetByCountry(country.CountryId, aliveOnly: true);
                deputyCount = deputies.Count;
                avgDeputyCorruption = deputyCount == 0 ? 0f : deputies.Average(d => EconomyCalculator.Clamp(d.Corruption / 100f, 0f, 1f));
                deputySalaryTotal = deputySalaryBase * deputyCount;
                deputyTheft = deputySalaryTotal * (decimal)avgDeputyCorruption * EconomyConfigData.GetDecimal("DeputyCorruptionFactor", 1m);
            }

            decimal upkeep = buildings.Sum(b => lifecycle.IsBuildingActive(b) ? b.UpkeepCost * lifecycle.GetBuildingUpkeepMultiplier(b) : 0m);
            decimal govSalaries = (decimal)availableWorkers * EconomyConfigData.GetDecimal("GovSalaryPerWorker", 0.2m);
            decimal pensionSpending = baseMechanics.PensionSpending;
            decimal socialProtectedSpending = baseMechanics.SocialProtectedSpending;
            decimal educationSpending = baseMechanics.EducationSpending;
            decimal unemploymentSupportSpending = baseMechanics.UnemploymentSupportSpending;
            decimal socialSpending = pensionSpending + socialProtectedSpending + educationSpending + unemploymentSupportSpending;
            decimal scienceSpending = (decimal)country.ResearchLevel * EconomyConfigData.GetDecimal("ScienceExpensePerLevel", 5m);
            decimal festivalCost = festivals.Sum(f => f.DurationTurns > 0 ? f.Cost / Math.Max(1, f.DurationTurns) : f.Cost);

            var budget = EconomyBudgetData.GetByCountry(country.CountryId);
            decimal budgetInfrastructure = Math.Max(0m, income * budget.InfrastructureShare);
            decimal budgetMilitary = Math.Max(0m, income * budget.MilitaryShare);
            decimal budgetScience = Math.Max(0m, income * budget.ScienceShare);
            decimal budgetSocial = Math.Max(0m, income * budget.SocialShare);

            decimal effectiveSocial = ApplyBudgetCap(socialSpending, budgetSocial);
            decimal effectiveScience = ApplyBudgetCap(scienceSpending, budgetScience);
            decimal militaryRequested = Math.Max(0m, baseMechanics.MilitaryRequiredSpending - baseMechanics.VassalFunding);
            decimal effectiveMilitary = ApplyBudgetCap(militaryRequested, budgetMilitary);
            decimal infrastructureSpending = Math.Max(0m, budgetInfrastructure);

            decimal expenses = upkeep + govSalaries + effectiveSocial + effectiveScience + festivalCost + effectiveMilitary + infrastructureSpending + deputySalaryTotal + deputyTheft;
            if (country.WarState)
            {
                decimal warCost = EconomyConfigData.GetDecimal("WarExtraExpensePerCitizen", 0.02m);
                expenses += warCost * country.Population;
            }
            if (country.MartialLaw)
            {
                decimal martialLawCost = EconomyConfigData.GetDecimal("MartialLawExpensePerCitizen", 0.02m);
                expenses += martialLawCost * country.Population;
            }
            decimal publicSystemsSpending = publicTick.TotalBudgetSpent;
            expenses += publicSystemsSpending;
            expenses += lawTreasuryDailyExpense;
            expenses *= (decimal)(1f + countryEffects.ExpenseModifier + diplomacy.ExpenseModifier);

            decimal corruptionLoss = productionValue * (decimal)(EconomyCalculator.Clamp(country.Corruption / 100f, 0f, 1f)) * EconomyConfigData.GetDecimal("CorruptionLossFactor", 0.25m);
            expenses += corruptionLoss;

            decimal stabilizationReserveFunding = ComputeStabilizationReserveFunding(
                country.CountryId,
                income,
                expenses,
                simulationDays);
            expenses += stabilizationReserveFunding;

            var finalMechanics = SocioMilitaryMechanics.Build(
                country,
                activeBuildings,
                totalJobs,
                unemploymentRateAdjusted,
                effectiveMilitary,
                context.MilitaryUnits,
                context.VassalObligations,
                baseMechanics.StateOrderBudget);

            ProgressMilitaryUnits(context.MilitaryUnits, finalMechanics, persistBuildings);

            if (country.WarState && country.ArmySize > 0)
            {
                float warLossFactor = EconomyConfigData.GetFloat("WarLossFactor", 0.015f);
                float casualtyRate = finalMechanics.DesertionRisk * warLossFactor;
                if (finalMechanics.QuickMobilization)
                    casualtyRate *= EconomyConfigData.GetFloat("QuickMobilizationCasualtyMultiplier", 1.4f);
                if (finalMechanics.AmmunitionIssued < 0.5f)
                    casualtyRate *= 1.2f;
                casualtyRate = EconomyCalculator.Clamp(casualtyRate, 0f, 0.2f);
                int warLosses = Math.Min(country.ArmySize, (int)Math.Round(country.ArmySize * casualtyRate, MidpointRounding.AwayFromZero));
                if (warLosses > 0)
                {
                    country.ArmySize -= warLosses;
                    country.Population = Math.Max(0L, country.Population - warLosses);
                }
            }

            country.ArmyMorale = EconomyCalculator.Clamp(
                country.ArmyMorale
                - (1f - finalMechanics.SupplyCoverage) * 12f
                - finalMechanics.DesertionRisk * 10f
                + (country.WarState ? -2f : 0.5f),
                0f,
                100f);

            country.ArmyLoyalty = EconomyCalculator.Clamp(
                country.ArmyLoyalty
                - finalMechanics.DesertionRisk * 6f
                - (country.MartialLaw ? 1.5f : 0f)
                + finalMechanics.CommanderLoyalty * 0.02f,
                0f,
                100f);

            country.Unrest = EconomyCalculator.Clamp(
                country.Unrest
                + finalMechanics.CrimeLevel * 0.06f
                + finalMechanics.DesertionRisk * 12f
                - finalMechanics.PoliceEffectiveness * 0.03f,
                0f,
                100f);

            float coupDelta = finalMechanics.CrimeLevel * 0.04f
                + (country.MartialLaw ? EconomyConfigData.GetFloat("MartialLawCoupRiskBonus", 4f) : 0f)
                + finalMechanics.MobilizationPressure * 10f
                - country.ArmyLoyalty * 0.03f;
            country.CoupRisk = EconomyCalculator.Clamp(country.CoupRisk + coupDelta, 0f, 100f);

            long incomeRounded = (long)Math.Round(income);
            long expensesRounded = (long)Math.Round(expenses);
            long netResultRaw = incomeRounded - expensesRounded;
            decimal publicBudgetShortfall = PublicSystemsEngine.ApplyTreasuryShortfallPenalty(
                country,
                context.PublicSystems,
                country.Treasury + netResultRaw,
                publicSystemsSpending);
            long netResult = netResultRaw;
            long deficitShortfall = 0;

            ApplyCorruptionAndStability(country, unemploymentRateAdjusted, taxRate, netResult, combinedMods, globalStabilityMod + sectorEffects.StabilityDelta);
            var populationLifecycle = UpdatePopulation(
                country,
                combinedMods,
                shortageReport.Impact.MortalityRateDelta + countryEffects.MortalityRateDelta + diplomacy.MortalityRateDelta);
            var updatedPopulationSnapshot = populationLifecycle.Snapshot;
            country.WorkRate = PopulationCalculator.ComputeWorkRate(country.Population, updatedPopulationSnapshot.WorkingAge);
            country.AvailableWorkers = PopulationCalculator.ComputeCivilianWorkforce(updatedPopulationSnapshot.WorkingAge, country.ArmySize, country.WarState);
            country.UnemploymentRate = EconomyCalculator.Clamp(finalMechanics.UnemploymentRate * 100f, 0f, 100f);
            country.IncomePerTurn = income;
            country.ExpensesPerTurn = expenses;
            country.CorruptionLosses = corruptionLoss;
            country.Inflation = EconomyCalculator.Clamp(country.Inflation + diplomacy.InflationDelta, 0f, 100f);
            if (country.Treasury + netResult < 0)
            {
                deficitShortfall = -(country.Treasury + netResult);
                netResult = -country.Treasury;
            }

            return new EconomyComputation
            {
                Income = income,
                Expenses = expenses,
                ProductionValue = productionValue,
                TradeIncome = tradeIncome,
                WagesTax = wagesTax,
                ProfitTax = profitTax,
                SocialContributionIncome = socialContributionIncome,
                AdditionalGrossIncomeTax = additionalGrossIncomeTax,
                AdditionalNetIncomeTax = additionalNetIncomeTax,
                AgreementExportIncome = agreementExportIncome,
                AgreementImportCost = agreementImportCost,
                CitizenGrossIncome = citizenGrossIncome,
                CitizenNetIncome = citizenNetIncome,
                MainIncomeTaxRate = mainIncomeTaxRate,
                SocialContributionRate = socialContributionRate,
                AdditionalGrossIncomeTaxRate = additionalGrossIncomeTaxRate,
                AdditionalNetIncomeTaxRate = additionalNetIncomeTaxRate,
                Upkeep = upkeep,
                GovSalaries = govSalaries,
                SocialSpending = effectiveSocial,
                PensionSpending = pensionSpending,
                SocialProtectedSpending = socialProtectedSpending,
                EducationSpending = educationSpending,
                UnemploymentSupportSpending = unemploymentSupportSpending,
                ScienceSpending = effectiveScience,
                ResearchBonus = combinedMods.ResearchBonus,
                FestivalCost = festivalCost,
                MilitaryUpkeep = effectiveMilitary,
                MilitarySalarySpending = baseMechanics.MilitarySalarySpending,
                MilitarySupplySpending = baseMechanics.MilitarySupplySpending,
                StateOrderBudget = baseMechanics.StateOrderBudget,
                InfrastructureSpending = infrastructureSpending,
                CorruptionLoss = corruptionLoss,
                DeputySalaryTotal = deputySalaryTotal,
                DeputyTheft = deputyTheft,
                VassalFunding = baseMechanics.VassalFunding,
                StabilizationReserveFunding = stabilizationReserveFunding,
                StateCompanyDividends = stateCompanyDividends,
                LawTreasuryDailyIncome = lawTreasuryDailyIncome,
                LawTreasuryDailyExpense = lawTreasuryDailyExpense,
                CompanyPayroll = companyPayroll,
                CompanyRevenue = companyRevenue,
                CompanyResourceRequired = companyResourceDemand.TotalRequired,
                CompanyResourceConsumed = companyResourceDemand.TotalConsumed,
                CompanyResourceCoverage = companyResourceDemand.CoveragePercent,
                ImportLimitFactor = lawPolicy.ImportLimitFactor,
                ExportLimitFactor = lawPolicy.ExportLimitFactor,
                CompanyResourceUseFactor = lawPolicy.CompanyResourceUseFactor,
                BudgetInfrastructure = budgetInfrastructure,
                BudgetMilitary = budgetMilitary,
                BudgetScience = budgetScience,
                BudgetSocial = budgetSocial,
                PublicHealthBudgetSpent = publicTick.HealthBudgetSpent,
                PublicEducationBudgetSpent = publicTick.EducationBudgetSpent,
                PublicIntlBudgetSpent = publicTick.IntlBudgetSpent,
                PublicEmergencyHealthSpend = publicTick.EmergencyHealthSpent,
                PublicEmergencyEducationSpend = publicTick.EmergencyEducationSpent,
                PublicTotalBudgetSpent = publicSystemsSpending,
                PublicBudgetDebt = context.PublicSystems.BudgetDebt,
                PublicBudgetShortfall = publicBudgetShortfall,
                PublicProductionMultiplier = publicTick.ProductionMultiplier,
                PublicHealthNeed = publicTick.HealthNeed,
                PublicActiveDiseases = publicTick.ActiveDiseases,
                PublicHealthPublic = context.PublicSystems.HealthPublic,
                PublicHealthSanitation = context.PublicSystems.HealthSanitation,
                PublicHealthCapacity = context.PublicSystems.HealthCapacity,
                PublicHealthTrust = context.PublicSystems.HealthTrust,
                PublicEduLiteracy = context.PublicSystems.EduLiteracy,
                PublicEduQuality = context.PublicSystems.EduQuality,
                PublicEduSpecialists = context.PublicSystems.EduSpecialists,
                PublicEduCohesion = context.PublicSystems.EduCohesion,
                PublicIntlPrestige = context.PublicSystems.IntlPrestige,
                PublicTickNotes = publicTick.TickNotes,
                EmploymentRate = employmentRate,
                UnemploymentRate = finalMechanics.UnemploymentRate,
                ProductionEfficiency = productionEfficiency,
                WorkingAgePopulation = updatedPopulationSnapshot.WorkingAge,
                EmployedWorkers = finalMechanics.EmployedWorkers,
                UnemployedWorkers = finalMechanics.UnemployedWorkers,
                ChildrenPopulation = updatedPopulationSnapshot.Children,
                PensionersPopulation = updatedPopulationSnapshot.Pensioners,
                SocialProtectedPopulation = updatedPopulationSnapshot.SocialProtected,
                PopulationBirths = populationLifecycle.Stats.Births,
                PopulationChildToWorking = populationLifecycle.Stats.ChildToWorking,
                PopulationWorkingToPensioners = populationLifecycle.Stats.WorkingToPensioners,
                PopulationWorkingToSocial = populationLifecycle.Stats.WorkingToSocial,
                PopulationPensionersToSocial = populationLifecycle.Stats.PensionersToSocial,
                PopulationDeathsChildren = populationLifecycle.Stats.DeathsChildren,
                PopulationDeathsWorking = populationLifecycle.Stats.DeathsWorking,
                PopulationDeathsPensioners = populationLifecycle.Stats.DeathsPensioners,
                PopulationDeathsSocial = populationLifecycle.Stats.DeathsSocial,
                PopulationGrowthReduction = populationLifecycle.Stats.GrowthReduction,
                PopulationStartChildren = populationLifecycle.Stats.StartChildren,
                PopulationStartWorkingAge = populationLifecycle.Stats.StartWorkingAge,
                PopulationStartPensioners = populationLifecycle.Stats.StartPensioners,
                PopulationStartSocialProtected = populationLifecycle.Stats.StartSocialProtected,
                PopulationEndChildren = populationLifecycle.Stats.EndChildren,
                PopulationEndWorkingAge = populationLifecycle.Stats.EndWorkingAge,
                PopulationEndPensioners = populationLifecycle.Stats.EndPensioners,
                PopulationEndSocialProtected = populationLifecycle.Stats.EndSocialProtected,
                WeaponQuality = finalMechanics.WeaponQuality,
                AmmunitionIssued = finalMechanics.AmmunitionIssued,
                SupplyCoverage = finalMechanics.SupplyCoverage,
                FormationTimeMultiplier = finalMechanics.FormationTimeMultiplier,
                CombatEffectiveness = finalMechanics.CombatEffectiveness,
                ArmyBonus = combinedMods.ArmyBonus,
                CrimeLevel = finalMechanics.CrimeLevel,
                PoliceEffectiveness = finalMechanics.PoliceEffectiveness,
                MobilizationPressure = finalMechanics.MobilizationPressure,
                DesertionRisk = finalMechanics.DesertionRisk,
                VassalRegiments = finalMechanics.VassalRegiments,
                VassalTroops = finalMechanics.VassalTroops,
                UnitStructure = finalMechanics.UnitStructure,
                Market = market,
                ShortageReport = shortageReport,
                NetResult = netResult,
                DeficitShortfall = deficitShortfall,
                SectorDistribution = ComputeSectorDistribution(country, buildings, availableWorkers, totalJobs)
            };
        }
        private sealed class LifecycleResult
        {
            public long TotalJobs { get; set; }
            public SectorDistribution SectorDistribution { get; set; } = new();
            private readonly Dictionary<int, string> _statusById = new();
            private readonly Dictionary<int, int> _degradationById = new();

            public void Set(int buildingId, string status, int degradationLevel)
            {
                _statusById[buildingId] = status;
                _degradationById[buildingId] = degradationLevel;
            }

            public bool IsBuildingActive(EconomyBuildingRecord b)
            {
                if (b == null)
                    return false;
                if (!_statusById.TryGetValue(b.BuildingId, out var status))
                    status = b.BuildingStatus;
                return status == BuildingStatus.Active.ToString() || status == BuildingStatus.Degraded.ToString();
            }

            public decimal GetBuildingProductionMultiplier(EconomyBuildingRecord b)
            {
                if (b == null)
                    return 1m;
                if (!_degradationById.TryGetValue(b.BuildingId, out int level))
                    level = b.DegradationLevel;
                float stepPenalty = EconomyConfigData.GetFloat("BuildingDegradeStepPenalty", 0.1f);
                float penalty = Math.Min(0.9f, Math.Max(0f, level * stepPenalty));
                return (decimal)(1f - penalty);
            }

            public decimal GetBuildingUpkeepMultiplier(EconomyBuildingRecord b)
            {
                if (b == null)
                    return 1m;
                if (!_degradationById.TryGetValue(b.BuildingId, out int level))
                    level = b.DegradationLevel;
                float stepPenalty = EconomyConfigData.GetFloat("BuildingDegradeStepPenalty", 0.1f);
                float penalty = Math.Min(1f, Math.Max(0f, level * stepPenalty));
                return (decimal)(1f + penalty);
            }
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

        private static decimal ApplyBudgetCap(decimal baseAmount, decimal budgeted)
        {
            if (baseAmount <= 0m)
                return 0m;
            if (budgeted <= 0m)
                return 0m;
            return budgeted >= baseAmount ? baseAmount : budgeted;
        }

        private sealed class CompanyTurnResult
        {
            public bool HasCompanies { get; set; }
            public decimal Payroll { get; set; }
            public decimal Revenue { get; set; }
            public decimal ProfitTax { get; set; }
            public decimal StateDividends { get; set; }
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static decimal ComputeStabilizationReserveFunding(int countryId, decimal income, decimal expenses, int simulationDays)
        {
            decimal enabled = EconomyConfigData.GetDecimalForCountry(countryId, NetStabilizerEnabledConfigKey, 1m);
            if (enabled < 0.5m)
                return 0m;

            int days = Math.Max(1, simulationDays);
            decimal targetDailyNet = EconomyConfigData.GetDecimalForCountry(countryId, NetStabilizerTargetDailyNetConfigKey, 5000m);
            decimal toleranceDailyNet = Math.Max(0m, EconomyConfigData.GetDecimalForCountry(countryId, NetStabilizerToleranceDailyNetConfigKey, 500m));
            decimal sinkRatio = ClampDecimal(EconomyConfigData.GetDecimalForCountry(countryId, NetStabilizerSinkRatioConfigKey, 1m), 0m, 1m);
            decimal maxExcessSpendShare = ClampDecimal(EconomyConfigData.GetDecimalForCountry(countryId, NetStabilizerMaxExcessSpendShareConfigKey, 1m), 0m, 1m);

            decimal currentNet = income - expenses;
            decimal upperBound = (targetDailyNet + toleranceDailyNet) * days;
            if (currentNet <= upperBound)
                return 0m;

            decimal excess = currentNet - upperBound;
            decimal reserveFunding = excess * sinkRatio;
            decimal maxAllowedFunding = excess * maxExcessSpendShare;
            if (reserveFunding > maxAllowedFunding)
                reserveFunding = maxAllowedFunding;

            reserveFunding = Math.Max(0m, reserveFunding);
            return decimal.Round(reserveFunding, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal GetProductionAutoGrowthFactorForCountry(int countryId)
        {
            decimal enabled = EconomyConfigData.GetDecimalForCountry(countryId, ProductionAutoGrowthEnabledConfigKey, 1m);
            if (enabled < 0.5m)
                return 0m;

            decimal configuredFactor = EconomyConfigData.GetDecimalForCountry(countryId, ProductionAutoGrowthFactorConfigKey, 0.15m);
            return ClampDecimal(configuredFactor, 0m, 1m);
        }

        private static float ComputeCompanyBudgetPressureModifier(List<EconomyCompanyRecord> companies, int days, int countryId)
        {
            if (companies == null || companies.Count == 0)
                return 1f;

            var active = companies.Where(c => c.IsActive).ToList();
            if (active.Count == 0)
                return 1f;

            int simDays = Math.Max(1, days);
            decimal growthFactor = GetProductionAutoGrowthFactorForCountry(countryId);
            decimal lowThreshold = Math.Max(0.1m, EconomyConfigData.GetDecimal("CompanyBudgetShrinkThresholdDays", 1.5m));
            decimal growthThreshold = Math.Max(lowThreshold + 0.1m, EconomyConfigData.GetDecimal("CompanyBudgetGrowthThresholdDays", 6m));

            decimal scoreSum = 0m;
            foreach (var company in active)
            {
                decimal salary = Math.Max(0.01m, company.BaseSalary);
                int jobs = Math.Max(1, company.JobsOffered);
                decimal payrollNeed = salary * jobs * simDays;
                decimal budgetRatio = payrollNeed <= 0m ? 1m : Math.Max(0m, company.Budget) / payrollNeed;

                decimal score;
                if (budgetRatio < lowThreshold)
                {
                    decimal normalized = budgetRatio / lowThreshold;
                    score = 0.65m + (normalized * 0.35m);
                }
                else if (budgetRatio > growthThreshold)
                {
                    decimal extra = Math.Min(0.15m, (budgetRatio - growthThreshold) * 0.02m) * growthFactor;
                    score = 1m + extra;
                }
                else
                {
                    score = 1m;
                }

                scoreSum += ClampDecimal(score, 0.55m, 1.2m);
            }

            decimal avg = scoreSum / active.Count;
            return EconomyCalculator.Clamp((float)avg, 0.65f, 1.15f);
        }

        private static CompanyTurnResult ProcessCompanyEconomyTurn(
            CountryRecord country,
            List<EconomyCompanyRecord> companies,
            SocioMilitarySnapshot mechanics,
            decimal productionValue,
            decimal tradeIncome,
            float taxEfficiency,
            decimal companySalaryMultiplier,
            int days,
            bool persistCompanies,
            IReadOnlyDictionary<int, decimal> resourceCoverageByCompany)
        {
            var result = new CompanyTurnResult();
            if (companies == null || companies.Count == 0)
                return result;

            var active = companies.Where(c => c.IsActive).ToList();
            if (active.Count == 0)
                return result;

            result.HasCompanies = true;
            int simDays = Math.Max(1, days);

            decimal revenueShare = ClampDecimal(EconomyConfigData.GetDecimal("CompanyRevenueShare", 0.82m), 0.1m, 1m);
            decimal operationalShare = ClampDecimal(EconomyConfigData.GetDecimal("CompanyOperationalCostShare", 0.5m), 0m, 0.95m);
            decimal fixedWorkerCost = Math.Max(0m, EconomyConfigData.GetDecimal("CompanyFixedJobCostPerWorker", 0.03m));
            decimal stateDividendShare = ClampDecimal(EconomyConfigData.GetDecimal("StateCompanyDividendShare", 0.5m), 0m, 1m);
            decimal stateOrderCompanyShare = ClampDecimal(EconomyConfigData.GetDecimal("StateOrderCompanyAllocationShare", 0.9m), 0m, 1m);

            long totalJobs = active.Sum(c => Math.Max(0, c.JobsOffered));
            decimal jobsDenominator = totalJobs > 0 ? totalJobs : active.Count;
            decimal distributableRevenue = Math.Max(0m, productionValue + tradeIncome) * revenueShare;
            decimal stateOrderPool = Math.Max(0m, mechanics.StateOrderBudget) * stateOrderCompanyShare;

            decimal militaryWeightTotal = active.Sum(ComputeCompanyMilitaryWeight);
            if (militaryWeightTotal <= 0m)
                militaryWeightTotal = 1m;

            var sectorRevenueMultiplierCache = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var sectorTaxOverrideCache = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

            foreach (var company in active)
            {
                decimal jobsShare = jobsDenominator <= 0m
                    ? (1m / active.Count)
                    : (Math.Max(0, company.JobsOffered) / jobsDenominator);

                decimal employedForCompany = Math.Max(0m, mechanics.EmployedWorkers * jobsShare);
                decimal salary = Math.Max(0.0001m, company.BaseSalary * ClampDecimal(companySalaryMultiplier, 0m, 20m));
                decimal payroll = employedForCompany * salary * simDays;

                string sectorKey = NormalizeCompanySectorKey(company.ProductionType);
                decimal typeMultiplier = ResolveCompanyRevenueMultiplier(company, country.WarState);
                decimal sectorRevenueMultiplier = ResolveSectorRevenueMultiplier(country.CountryId, sectorKey, sectorRevenueMultiplierCache);
                decimal revenue = distributableRevenue * jobsShare * typeMultiplier * sectorRevenueMultiplier;

                decimal militaryWeight = ComputeCompanyMilitaryWeight(company);
                decimal stateOrderIncome = stateOrderPool * (militaryWeight / militaryWeightTotal);

                decimal resourceCoverage = 1m;
                if (resourceCoverageByCompany != null &&
                    resourceCoverageByCompany.TryGetValue(company.CompanyId, out var coverage))
                {
                    resourceCoverage = ClampDecimal(coverage, 0m, 1m);
                }

                revenue *= resourceCoverage;
                stateOrderIncome *= resourceCoverage;

                decimal operationalCost = (revenue * operationalShare) + (Math.Max(0, company.JobsOffered) * fixedWorkerCost * simDays);
                decimal profitBeforeTax = revenue + stateOrderIncome - payroll - operationalCost;

                decimal? sectorTaxOverride = ResolveSectorTaxOverride(country.CountryId, sectorKey, sectorTaxOverrideCache);
                decimal effectiveTaxRate = sectorTaxOverride ?? Math.Max(0m, company.TaxRate);
                decimal companyTaxRate = ClampDecimal(effectiveTaxRate, 0m, 0.95m);
                decimal profitTax = Math.Max(0m, profitBeforeTax) * companyTaxRate * (decimal)EconomyCalculator.Clamp(taxEfficiency, 0f, 1f);
                decimal profitAfterTax = profitBeforeTax - profitTax;
                decimal stateDividends = company.IsStateCompany
                    ? Math.Max(0m, profitAfterTax) * stateDividendShare
                    : 0m;

                decimal budgetDelta = profitAfterTax - stateDividends;
                decimal nextBudget = Math.Max(0m, company.Budget + budgetDelta);
                int nextJobs = AdjustCompanyJobs(company, nextBudget, simDays, companySalaryMultiplier);
                bool nextActive = nextJobs > 0 || nextBudget > 0m;

                if (persistCompanies)
                {
                    if (decimal.Round(nextBudget, 2, MidpointRounding.AwayFromZero) != decimal.Round(company.Budget, 2, MidpointRounding.AwayFromZero))
                    {
                        EconomyCompaniesData.UpdateCompanyField(
                            company.CompanyId,
                            country.CountryId,
                            "Budget",
                            decimal.Round(nextBudget, 2, MidpointRounding.AwayFromZero),
                            SqlDbType.Decimal);
                    }

                    if (nextJobs != company.JobsOffered)
                    {
                        EconomyCompaniesData.UpdateCompanyField(
                            company.CompanyId,
                            country.CountryId,
                            "JobsOffered",
                            nextJobs,
                            SqlDbType.Int);
                    }

                    if (nextActive != company.IsActive)
                    {
                        EconomyCompaniesData.UpdateCompanyField(
                            company.CompanyId,
                            country.CountryId,
                            "IsActive",
                            nextActive,
                            SqlDbType.Bit);
                    }
                }

                company.Budget = nextBudget;
                company.JobsOffered = nextJobs;
                company.IsActive = nextActive;

                result.Payroll += payroll;
                result.Revenue += revenue + stateOrderIncome;
                result.ProfitTax += profitTax;
                result.StateDividends += stateDividends;
            }

            return result;
        }

        private static decimal ResolveCompanyRevenueMultiplier(EconomyCompanyRecord company, bool warState)
        {
            string type = (company.ProductionType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(type))
                return 1m;

            if (type.Equals("Agriculture", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Food", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Ферма", StringComparison.OrdinalIgnoreCase))
                return 0.95m;

            if (type.Equals("Industry", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("General", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Manufacturing", StringComparison.OrdinalIgnoreCase))
                return 1m;

            if (IsMilitaryOrLogisticsProduction(type))
                return warState ? 1.22m : 1.08m;

            if (type.Equals("Services", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Finance", StringComparison.OrdinalIgnoreCase))
                return warState ? 0.92m : 1.03m;

            return 1m;
        }

        private static string NormalizeCompanySectorKey(string? productionType)
        {
            if (string.IsNullOrWhiteSpace(productionType))
                return "General";

            string value = productionType.Trim().ToLowerInvariant();
            string compact = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

            switch (compact)
            {
                case "general":
                case "загальне":
                    return "General";
                case "food":
                case "харчі":
                case "agriculture":
                case "farm":
                case "ферма":
                    return "Food";
                case "industry":
                case "промисловість":
                case "manufacturing":
                    return "Industry";
                case "services":
                case "послуги":
                case "finance":
                case "financial":
                    return "Services";
                case "logistics":
                case "логістика":
                    return "Logistics";
                case "weapons":
                case "arms":
                case "зброя":
                case "озброєння":
                    return "Weapons";
                case "militaryindustry":
                case "military":
                case "військове":
                case "військпром":
                case "оборонпром":
                case "defense":
                    return "MilitaryIndustry";
                case "siegeworks":
                case "siege":
                case "облогова":
                case "облогове":
                    return "SiegeWorks";
                case "arcanearms":
                case "арканзброя":
                case "магічназброя":
                    return "ArcaneArms";
            }

            if (compact.Contains("weapon") || compact.Contains("arms") || compact.Contains("збро"))
                return "Weapons";
            if (compact.Contains("military") || compact.Contains("військ") || compact.Contains("оборон") || compact.Contains("defense"))
                return "MilitaryIndustry";
            if (compact.Contains("siege") || compact.Contains("облог"))
                return "SiegeWorks";
            if (compact.Contains("arcane") || compact.Contains("аркан") || compact.Contains("магіч"))
                return "ArcaneArms";
            if (compact.Contains("logistic") || compact.Contains("логіст"))
                return "Logistics";
            if (compact.Contains("service") || compact.Contains("послуг") || compact.Contains("financ"))
                return "Services";
            if (compact.Contains("agri") || compact.Contains("farm") || compact.Contains("харч"))
                return "Food";
            if (compact.Contains("indust") || compact.Contains("manufact") || compact.Contains("промисл"))
                return "Industry";

            return "General";
        }

        private static string BuildSectorRevenueMultiplierConfigKey(string sectorKey)
        {
            return $"{SectorRevenueMultiplierConfigPrefix}{NormalizeCompanySectorKey(sectorKey)}";
        }

        private static string BuildSectorTaxOverrideConfigKey(string sectorKey)
        {
            return $"{SectorTaxOverrideConfigPrefix}{NormalizeCompanySectorKey(sectorKey)}";
        }

        private static decimal ResolveSectorRevenueMultiplier(
            int countryId,
            string sectorKey,
            Dictionary<string, decimal> cache)
        {
            string normalizedSector = NormalizeCompanySectorKey(sectorKey);
            if (cache.TryGetValue(normalizedSector, out var cached))
                return cached;

            string key = BuildSectorRevenueMultiplierConfigKey(normalizedSector);
            decimal configured = EconomyConfigData.GetDecimalForCountry(countryId, key, 1m);
            decimal resolved = ClampDecimal(configured, SectorRevenueMultiplierMin, SectorRevenueMultiplierMax);
            cache[normalizedSector] = resolved;
            return resolved;
        }

        private static decimal? ResolveSectorTaxOverride(
            int countryId,
            string sectorKey,
            Dictionary<string, decimal?> cache)
        {
            string normalizedSector = NormalizeCompanySectorKey(sectorKey);
            if (cache.TryGetValue(normalizedSector, out var cached))
                return cached;

            string key = BuildSectorTaxOverrideConfigKey(normalizedSector);
            decimal configured = EconomyConfigData.GetDecimalForCountry(countryId, key, SectorTaxOverrideAutoValue);
            decimal? resolved = configured < 0m ? null : ClampDecimal(configured, 0m, 0.95m);
            cache[normalizedSector] = resolved;
            return resolved;
        }

        private static bool IsMilitaryOrLogisticsProduction(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            string normalized = type.Trim().ToLowerInvariant();
            return normalized.Equals("military")
                || normalized.Equals("militaryindustry")
                || normalized.Equals("siegeworks")
                || normalized.Equals("arcanearms")
                || normalized.Equals("defense")
                || normalized.Equals("arms")
                || normalized.Equals("logistics")
                || normalized.Equals("військова")
                || normalized.Equals("оборонна")
                || normalized.Equals("логістика")
                || normalized.Contains("weapon")
                || normalized.Contains("ammo")
                || normalized.Contains("armor")
                || normalized.Contains("siege")
                || normalized.Contains("arcane")
                || normalized.Contains("arsenal")
                || normalized.Contains("військ")
                || normalized.Contains("збро")
                || normalized.Contains("облог")
                || normalized.Contains("арк");
        }

        private static decimal ComputeCompanyMilitaryWeight(EconomyCompanyRecord company)
        {
            decimal shareWeight = ClampDecimal((decimal)Math.Max(0f, company.MilitaryProductionShare) / 100m, 0m, 1m);
            decimal typeBonus = IsMilitaryOrLogisticsProduction(company.ProductionType) ? 0.6m : 0m;
            decimal stateBonus = company.IsStateCompany ? 0.1m : 0m;
            return Math.Max(0.05m, shareWeight + typeBonus + stateBonus);
        }

        private static int AdjustCompanyJobs(EconomyCompanyRecord company, decimal nextBudget, int days, decimal companySalaryMultiplier)
        {
            int currentJobs = Math.Max(0, company.JobsOffered);
            int simDays = Math.Max(1, days);
            decimal salary = Math.Max(0.0001m, company.BaseSalary * ClampDecimal(companySalaryMultiplier, 0m, 20m));

            decimal shrinkThresholdDays = Math.Max(0.1m, EconomyConfigData.GetDecimal("CompanyBudgetShrinkThresholdDays", 1.5m));
            decimal growthThresholdDays = Math.Max(shrinkThresholdDays + 0.1m, EconomyConfigData.GetDecimal("CompanyBudgetGrowthThresholdDays", 6m));
            decimal growthFactor = GetProductionAutoGrowthFactorForCountry(company.CountryId);
            decimal growthRateBase = ClampDecimal(
                EconomyConfigData.GetDecimalForCountry(
                    company.CountryId,
                    "CompanyJobGrowthRate",
                    EconomyConfigData.GetDecimal("CompanyJobGrowthRate", 0.04m)),
                0m,
                0.3m);
            decimal growthRate = ClampDecimal(growthRateBase * growthFactor, 0m, 0.3m);
            decimal shrinkRate = ClampDecimal(EconomyConfigData.GetDecimal("CompanyJobShrinkRate", 0.08m), 0m, 0.6m);
            int minJobs = Math.Max(1, EconomyConfigData.GetInt("CompanyMinimumJobs", 5));

            decimal currentPayrollNeed = Math.Max(1, currentJobs) * salary * simDays;
            decimal budgetRatio = currentPayrollNeed <= 0m ? 1m : nextBudget / currentPayrollNeed;

            if (currentJobs <= 0)
            {
                if (growthFactor > 0m && budgetRatio >= growthThresholdDays)
                    return minJobs;
                return 0;
            }

            if (budgetRatio < shrinkThresholdDays)
            {
                int reduced = (int)Math.Floor(currentJobs * (1m - shrinkRate));
                return Math.Max(0, reduced);
            }

            if (budgetRatio > growthThresholdDays)
            {
                int increased = (int)Math.Ceiling(currentJobs * (1m + growthRate));
                return Math.Min(2_000_000, Math.Max(currentJobs, increased));
            }

            return currentJobs;
        }

        private LifecycleResult UpdateBuildingLifecycle(List<EconomyBuildingRecord> buildings, long availableWorkers, CountryRecord country, bool persistBuildings)
        {
            var result = new LifecycleResult();

            var ordered = buildings
                .OrderBy(GetBuildingLaborPriority)
                .ThenBy(b => Math.Max(0, b.JobsProvided))
                .ThenBy(b => b.BuildingId)
                .ToList();
            long workersRemaining = availableWorkers;
            long totalJobs = 0;

            foreach (var b in ordered)
            {
                string status = b.BuildingStatus;
                int inactiveTurns = b.InactiveTurns;
                int degradation = b.DegradationLevel;
                int buildTurns = Math.Max(0, b.BuildTurns);

                bool markedUnderConstruction = string.Equals(status, BuildingStatus.UnderConstruction.ToString(), StringComparison.OrdinalIgnoreCase);
                if (buildTurns > 0)
                {
                    status = BuildingStatus.UnderConstruction.ToString();
                    inactiveTurns = 0;
                    buildTurns = Math.Max(0, buildTurns - 1);
                    if (buildTurns <= 0)
                        status = BuildingStatus.Active.ToString();
                }
                else if (markedUnderConstruction)
                {
                     
                    status = BuildingStatus.Active.ToString();
                    inactiveTurns = 0;
                }

                bool isUnderConstruction = string.Equals(status, BuildingStatus.UnderConstruction.ToString(), StringComparison.OrdinalIgnoreCase) && buildTurns > 0;
                bool isAbandoned = string.Equals(status, BuildingStatus.Abandoned.ToString(), StringComparison.OrdinalIgnoreCase);
                bool hasWorkers = (!isUnderConstruction && !isAbandoned) && (b.JobsProvided <= 0 || workersRemaining >= b.JobsProvided);
                if (hasWorkers && b.JobsProvided > 0)
                    workersRemaining -= b.JobsProvided;

                if (!hasWorkers)
                {
                    if (!isUnderConstruction && !isAbandoned)
                    {
                        status = BuildingStatus.Inactive.ToString();
                        inactiveTurns += 1;
                    }
                }
                else if (!isUnderConstruction)
                {
                    inactiveTurns = 0;
                    if (degradation > 0)
                        status = BuildingStatus.Degraded.ToString();
                    else
                        status = BuildingStatus.Active.ToString();
                }

                int degradeTurns = (int)Math.Round(EconomyConfigData.GetFloat("BuildingInactiveDegradeTurns", 3f));
                int maxDegrade = (int)Math.Round(EconomyConfigData.GetFloat("BuildingMaxDegrade", 4f));
                if (inactiveTurns >= degradeTurns)
                {
                    degradation = Math.Min(maxDegrade, degradation + 1);
                    inactiveTurns = 0;
                }
                if (degradation >= maxDegrade)
                    status = BuildingStatus.Abandoned.ToString();

                result.Set(b.BuildingId, status, degradation);

                if (!isUnderConstruction && !string.Equals(status, BuildingStatus.Abandoned.ToString(), StringComparison.OrdinalIgnoreCase))
                    totalJobs += Math.Max(0, b.JobsProvided);

                if (persistBuildings)
                {
                    EconomyBuildingsData.UpdateBuildingLifecycle(b.BuildingId, country.CountryId, status, inactiveTurns, degradation, buildTurns);
                }

                b.BuildingStatus = status;
                b.InactiveTurns = inactiveTurns;
                b.DegradationLevel = degradation;
                b.BuildTurns = buildTurns;
            }

            result.TotalJobs = totalJobs;
            result.SectorDistribution = ComputeSectorDistribution(country, buildings, availableWorkers, totalJobs);
            return result;
        }

        private static int GetBuildingLaborPriority(EconomyBuildingRecord building)
        {
            if (building == null)
                return 100;

            string category = building.Category ?? string.Empty;
            string resource = building.ResourceType ?? string.Empty;

            if (category.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                if (resource.Equals("Food", StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (resource.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                    return 1;
                if (resource.Equals("Clothing", StringComparison.OrdinalIgnoreCase))
                    return 2;
                if (resource.Equals("Tools", StringComparison.OrdinalIgnoreCase))
                    return 3;
                if (resource.Equals("Goods", StringComparison.OrdinalIgnoreCase))
                    return 4;
                if (resource.Equals("Luxury", StringComparison.OrdinalIgnoreCase))
                    return 5;
                return 6;
            }

            if (category.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                return 7;
            if (category.Equals("Science", StringComparison.OrdinalIgnoreCase) || category.Equals("Culture", StringComparison.OrdinalIgnoreCase))
                return 8;
            if (category.Equals("Administration", StringComparison.OrdinalIgnoreCase) || category.Equals("Other", StringComparison.OrdinalIgnoreCase))
                return 9;
            if (category.Equals("Military", StringComparison.OrdinalIgnoreCase))
                return 10;

            return 11;
        }

        private static SectorDistribution ComputeSectorDistribution(CountryRecord country, List<EconomyBuildingRecord> buildings, long availableWorkers, long totalJobs)
        {
            int agricultureJobs = 0;
            int industryJobs = 0;
            int servicesJobs = 0;
            int scienceJobs = 0;
            int governmentJobs = 0;
            int militaryJobs = Math.Max(0, country.ArmySize);

            foreach (var b in buildings)
            {
                int jobs = Math.Max(0, b.JobsProvided);
                string category = b.Category ?? string.Empty;
                if (category.Equals("Science", StringComparison.OrdinalIgnoreCase))
                {
                    scienceJobs += jobs;
                }
                else if (category.Equals("Administration", StringComparison.OrdinalIgnoreCase))
                {
                    governmentJobs += jobs;
                }
                else if (category.Equals("Military", StringComparison.OrdinalIgnoreCase))
                {
                    militaryJobs += jobs;
                }
                else if (category.Equals("Culture", StringComparison.OrdinalIgnoreCase) || category.Equals("Other", StringComparison.OrdinalIgnoreCase) || category.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                {
                    servicesJobs += jobs;
                }
                else if (category.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(b.ResourceType, "Food", StringComparison.OrdinalIgnoreCase))
                        agricultureJobs += jobs;
                    else
                        industryJobs += jobs;
                }
                else
                {
                    servicesJobs += jobs;
                }
            }

            long totalJobsSafe = Math.Max(1L, totalJobs);
            float fillRatio = Math.Min(1f, availableWorkers / (float)totalJobsSafe);

            long agriculture = (long)Math.Round(agricultureJobs * fillRatio);
            long industry = (long)Math.Round(industryJobs * fillRatio);
            long services = (long)Math.Round(servicesJobs * fillRatio);
            long science = (long)Math.Round(scienceJobs * fillRatio);
            long government = (long)Math.Round(governmentJobs * fillRatio);
            long military = (long)Math.Round(militaryJobs * fillRatio);

            long employed = agriculture + industry + services + science + government + military;
            long unemployed = Math.Max(0L, availableWorkers - employed);

            long denom = Math.Max(1L, availableWorkers);
            return new SectorDistribution
            {
                AvailableWorkers = availableWorkers,
                TotalJobs = totalJobs,
                Unemployed = unemployed,
                Entries = new List<SectorDistributionEntry>
                {
                    new SectorDistributionEntry { Sector = "Agriculture", Workers = agriculture, Share = agriculture / (float)denom },
                    new SectorDistributionEntry { Sector = "Industry", Workers = industry, Share = industry / (float)denom },
                    new SectorDistributionEntry { Sector = "Services", Workers = services, Share = services / (float)denom },
                    new SectorDistributionEntry { Sector = "Science", Workers = science, Share = science / (float)denom },
                    new SectorDistributionEntry { Sector = "Government", Workers = government, Share = government / (float)denom },
                    new SectorDistributionEntry { Sector = "Military", Workers = military, Share = military / (float)denom },
                    new SectorDistributionEntry { Sector = "Unemployed", Workers = unemployed, Share = unemployed / (float)denom }
                }
            };
        }

        private sealed class SectorImbalanceEffects
        {
            public float ProductionModifier { get; set; }
            public float StabilityDelta { get; set; }
        }

        private static SectorImbalanceEffects ComputeSectorImbalanceEffects(SectorDistribution distribution)
        {
            var effects = new SectorImbalanceEffects();
            if (distribution == null || distribution.Entries.Count == 0)
                return effects;

            float agMin = EconomyConfigData.GetFloat("OptimalAgricultureMin", 0.15f);
            float sciMin = EconomyConfigData.GetFloat("OptimalScienceMin", 0.05f);
            float govMilMax = EconomyConfigData.GetFloat("OptimalGovMilMax", 0.25f);

            float ag = distribution.Entries.FirstOrDefault(e => e.Sector == "Agriculture")?.Share ?? 0f;
            float sci = distribution.Entries.FirstOrDefault(e => e.Sector == "Science")?.Share ?? 0f;
            float gov = distribution.Entries.FirstOrDefault(e => e.Sector == "Government")?.Share ?? 0f;
            float mil = distribution.Entries.FirstOrDefault(e => e.Sector == "Military")?.Share ?? 0f;

            float agGap = agMin - ag;
            if (agGap > 0f)
                effects.StabilityDelta -= Math.Min(5f, agGap * 100f * 0.2f);

            float sciGap = sciMin - sci;
            if (sciGap > 0f)
                effects.ProductionModifier -= Math.Min(0.2f, sciGap * 0.5f);

            float govMilGap = (gov + mil) - govMilMax;
            if (govMilGap > 0f)
                effects.ProductionModifier -= Math.Min(0.2f, govMilGap * 0.5f);

            return effects;
        }

        private static float SumGlobalModifier(List<EconomyGlobalModifierRecord> mods, Func<EconomyGlobalModifierRecord, float> selector)
        {
            if (mods == null || mods.Count == 0)
                return 0f;
            float sum = 0f;
            foreach (var m in mods)
                sum += selector(m);
            return sum;
        }

        private static bool IsBuildingStatusActive(string status)
        {
            return string.Equals(status, BuildingStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, BuildingStatus.Degraded.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBuildingEffectivelyActive(EconomyBuildingRecord building)
        {
            if (building == null || building.BuildTurns > 0)
                return false;
            return IsBuildingStatusActive(building.BuildingStatus);
        }

        private static EconomyModifiers AggregateFestivalModifiers(List<EconomyFestivalRecord> festivals, List<EconomyBuildingRecord> buildings)
        {
            festivals ??= new List<EconomyFestivalRecord>();
            buildings ??= new List<EconomyBuildingRecord>();

            var activeBuildings = buildings.Where(IsBuildingEffectivelyActive).ToList();

            float stability = festivals.Sum(f => f.StabilityBonus) + activeBuildings.Sum(b => b.StabilityBonus);
            float populationGrowth = festivals.Sum(f => f.PopulationGrowthBonus) + activeBuildings.Sum(b => b.PopulationGrowthBonus);
            float productionPenalty = festivals.Sum(f => Math.Max(0f, f.ProductionPenalty));
            float incomePenalty = festivals.Sum(f => Math.Max(0f, f.IncomePenalty));
            float corruptionReduction = activeBuildings.Sum(b => b.CorruptionReduction);
            float festivalBoost = activeBuildings.Sum(b => b.FestivalBoost);
            float researchBonus = activeBuildings.Sum(b => b.ResearchBonus);
            int armyBonus = activeBuildings.Sum(b => Math.Max(0, b.ArmyBonus));

            return new EconomyModifiers
            {
                StabilityBonus = stability,
                PopulationGrowthBonus = populationGrowth,
                ProductionPenalty = productionPenalty,
                IncomePenalty = incomePenalty,
                FestivalBoost = festivalBoost,
                CorruptionReduction = corruptionReduction,
                ResearchBonus = researchBonus,
                ArmyBonus = armyBonus
            };
        }

        private static DemandConfig BuildDemandConfig()
        {
            decimal food = EconomyConfigData.GetDecimal("DemandFoodPerPerson", 1m);
            decimal clothing = EconomyConfigData.GetDecimal("DemandClothingPerPerson", 0.3m);
            decimal housing = EconomyConfigData.GetDecimal("DemandHousingPerPerson", 0.1m);
            decimal tools = EconomyConfigData.GetDecimal("DemandToolsPerWorker", 0.2m);
            decimal luxury = EconomyConfigData.GetDecimal("DemandLuxuryPerPerson", 0.15m);

            var config = new DemandConfig
            {
                ClothingWinterMultiplier = EconomyConfigData.GetDecimal("DemandClothingWinterMultiplier", 1.25m),
                HousingPerNewPerson = EconomyConfigData.GetDecimal("DemandHousingPerNewPerson", 0.5m),
                LuxuryHappinessWeight = EconomyConfigData.GetFloat("DemandLuxuryHappinessWeight", 0.6f),
                LuxuryWealthWeight = EconomyConfigData.GetFloat("DemandLuxuryWealthWeight", 0.4f),
                BlackMarketMinFactor = EconomyConfigData.GetDecimal("BlackMarketMinFactor", 0.3m),
                BlackMarketMaxFactor = EconomyConfigData.GetDecimal("BlackMarketMaxFactor", 0.7m),
                BlackMarketPriceMultiplier = EconomyConfigData.GetDecimal("BlackMarketPriceMultiplier", 1.5m),
                BlackMarketBaseSuccess = EconomyConfigData.GetFloat("BlackMarketBaseSuccess", 0.2f),
                BlackMarketCorruptionWeight = EconomyConfigData.GetFloat("BlackMarketCorruptionWeight", 0.6f),
                BlackMarketRiskWeight = EconomyConfigData.GetFloat("BlackMarketRiskWeight", 0.3f),
                ChildrenRates = new DemandProfile
                {
                    Food = food * 0.8m,
                    Clothing = clothing * 0.7m,
                    Housing = housing * 0.6m,
                    Tools = tools * 0.2m,
                    Luxury = luxury * 0.4m
                },
                WorkingRates = new DemandProfile
                {
                    Food = food * 1.0m,
                    Clothing = clothing * 1.0m,
                    Housing = housing * 1.0m,
                    Tools = tools * 1.0m,
                    Luxury = luxury * 1.0m
                },
                ElderlyRates = new DemandProfile
                {
                    Food = food * 0.9m,
                    Clothing = clothing * 0.9m,
                    Housing = housing * 1.1m,
                    Tools = tools * 0.2m,
                    Luxury = luxury * 0.8m
                }
            };

            return config;
        }

        private static DemandImpactConfig BuildDemandImpactConfig()
        {
            var config = DemandImpactConfig.CreateDefault();
            config.HappinessScale = EconomyConfigData.GetFloat("ShortageHappinessScale", 10f);
            config.MortalityScale = EconomyConfigData.GetFloat("ShortageMortalityScale", 0.02f);
            config.ProductivityScale = EconomyConfigData.GetFloat("ShortageProductivityScale", 0.15f);
            config.ShortageScoreScale = EconomyConfigData.GetFloat("ShortageScoreScale", 100f);
            return config;
        }

        private static DemandInput BuildDemandInput(
            CountryRecord country,
            EconomyModifiers festivalMods,
            List<EconomyBuildingRecord> buildings,
            List<EconomyGlobalModifierRecord> globalModifiers,
            Dictionary<string, EconomyResourceRecord> resources,
            float happinessDelta,
            float blackMarketRiskDelta,
            decimal inflationPriceMultiplier)
        {
            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long children = population.Children;
            long workingAge = population.WorkingAge;
            long elderly = Math.Max(0L, population.Pensioners + population.SocialProtected);

            float growthPct = country.PopulationGrowth + festivalMods.PopulationGrowthBonus;
            float growthRate = growthPct / 100f;

            bool luxuryEnabled = EconomyConfigData.GetInt("DemandLuxuryEnabled", 1) > 0;
            float blackMarketRisk = EconomyConfigData.GetFloat("BlackMarketRiskDefault", 0.3f) + blackMarketRiskDelta;

            var priceMap = new Dictionary<ResourceType, decimal>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (resources.TryGetValue(type.ToString(), out var res))
                    priceMap[type] = res.BasePrice * inflationPriceMultiplier;
            }

            var input = new DemandInput
            {
                Population = new PopulationBreakdown
                {
                    Children = children,
                    WorkingAge = workingAge,
                    Elderly = elderly,
                    GrowthRatePerTurn = growthRate
                },
                Season = GetCurrentSeason(),
                AverageHappiness = EconomyCalculator.Clamp(country.Happiness + happinessDelta, 0f, 100f),
                AverageWealthIndex = ComputeWealthIndex(country),
                Corruption = country.Corruption,
                BlackMarketRisk = EconomyCalculator.Clamp(blackMarketRisk, 0f, 1f),
                LuxuryEnabled = luxuryEnabled,
                Modifiers = BuildDemandModifiers(globalModifiers, resources),
                ResourceBasePrices = priceMap
            };

            return input;
        }

        private static List<BuildingResourceFlowPlan> BuildProductionPlans(
            List<EconomyBuildingRecord> buildings,
            LifecycleResult lifecycle,
            float productionEfficiency,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            var plans = new List<BuildingResourceFlowPlan>();
            foreach (var b in buildings)
            {
                if (!HasResourceOutput(b))
                    continue;
                if (!lifecycle.IsBuildingActive(b))
                    continue;

                bool ignoreByResource = resources != null &&
                                        resources.TryGetValue(b.ResourceType!, out var resourceRecord) &&
                                        resourceRecord.IgnoreProductionEfficiency;
                decimal efficiencyFactor = (b.IgnoreProductionEfficiency || ignoreByResource)
                    ? 1m
                    : (decimal)productionEfficiency;

                decimal potentialOutput = b.BaseProduction * efficiencyFactor * lifecycle.GetBuildingProductionMultiplier(b);
                if (potentialOutput <= 0m)
                    continue;

                plans.Add(new BuildingResourceFlowPlan
                {
                    BuildingId = b.BuildingId,
                    OutputResourceType = b.ResourceType!,
                    OutputAmount = potentialOutput,
                    InputResourceType = b.InputResourceType,
                    InputAmount = Math.Max(0m, b.InputAmount)
                });
            }

            return plans;
        }

        private static SupplySnapshot BuildSupplySnapshot(
            IReadOnlyDictionary<string, decimal> availableByResource,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            var snapshot = new SupplySnapshot();
            if (availableByResource == null || availableByResource.Count == 0)
                return snapshot;

            foreach (var pair in availableByResource)
            {
                decimal value = pair.Value;
                if (value <= 0m)
                    continue;

                if (!TrackedResourceResolver.TryResolveTrackedType(pair.Key, resources, out var trackedType))
                    continue;

                if (snapshot.Supply.TryGetValue(trackedType, out var current))
                    snapshot.Supply[trackedType] = current + value;
                else
                    snapshot.Supply[trackedType] = value;
            }

            return snapshot;
        }

        private static Dictionary<string, decimal> ConvertResourceFlowDictionary(IReadOnlyDictionary<string, decimal> flowByResource)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (flowByResource == null || flowByResource.Count == 0)
                return result;

            foreach (var pair in flowByResource)
            {
                if (pair.Value <= 0m)
                    continue;
                string key = NormalizeResourceFlowKey(pair.Key);
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                result[key] = pair.Value;
            }

            return result;
        }

        private static CompanyResourceDemandResult ApplyCompanyResourceDemand(
            List<EconomyCompanyRecord> companies,
            Dictionary<string, decimal> availableByResource,
            int days,
            decimal companyResourceUseFactor)
        {
            var result = new CompanyResourceDemandResult();
            if (companies == null || companies.Count == 0)
                return result;

            int simDays = Math.Max(1, days);
            decimal policyFactor = ClampDecimal(companyResourceUseFactor, 0m, 1m);
            var active = companies.Where(c => c.IsActive).ToList();
            if (active.Count == 0)
                return result;

            foreach (var company in active)
            {
                decimal coverage = 1m;
                decimal perDayNeed = Math.Max(0m, company.InputAmount);
                string resourceKey = NormalizeResourceFlowKey(company.InputResourceType);

                if (perDayNeed > 0m && !string.IsNullOrWhiteSpace(resourceKey))
                {
                    decimal baseRequired = perDayNeed * simDays;
                    decimal allowedRequired = baseRequired * policyFactor;
                    result.TotalRequired += baseRequired;

                    decimal available = 0m;
                    if (availableByResource != null && availableByResource.TryGetValue(resourceKey, out var current))
                        available = Math.Max(0m, current);

                    decimal consumed = Math.Min(available, Math.Max(0m, allowedRequired));
                    if (availableByResource != null)
                        availableByResource[resourceKey] = Math.Max(0m, available - consumed);

                    result.TotalConsumed += consumed;
                    coverage = baseRequired <= 0m ? 1m : (consumed / baseRequired);
                }
                else if (perDayNeed > 0m)
                {
                    result.TotalRequired += perDayNeed * simDays;
                    coverage = 0m;
                }

                result.CoverageByCompanyId[company.CompanyId] = ClampDecimal(coverage, 0m, 1m);
            }

            return result;
        }

        private static string NormalizeResourceFlowKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static TradeAgreementEffects ComputeTradeAgreementEffects(
            List<EconomyTradeAgreementRecord> tradeAgreements,
            Dictionary<string, EconomyResourceRecord> resources,
            Dictionary<int, EconomyCountrySanctionRecord> partnerSanctions,
            Dictionary<string, EconomyCountryProductSanctionRecord> productSanctionsMap,
            float sanctionsModifier,
            float warTradeModifier,
            decimal inflationPriceMultiplier,
            int? previewDays,
            decimal importLimitFactor,
            decimal exportLimitFactor,
            IReadOnlyDictionary<string, decimal> importLimitByResource,
            IReadOnlyDictionary<string, decimal> exportLimitByResource)
        {
            var effects = new TradeAgreementEffects();
            if (tradeAgreements == null || tradeAgreements.Count == 0)
                return effects;

            decimal importLawFactorGlobal = ClampDecimal(importLimitFactor, 0m, 1m);
            decimal exportLawFactorGlobal = ClampDecimal(exportLimitFactor, 0m, 1m);

            foreach (var agreement in tradeAgreements)
            {
                int simulationDays = Math.Max(1, previewDays ?? 1);

                decimal exportBasePrice = 0m;
                if (!string.IsNullOrWhiteSpace(agreement.ExportResourceType) &&
                    resources.TryGetValue(agreement.ExportResourceType, out var exportRes))
                {
                    exportBasePrice = exportRes.BasePrice;
                }

                decimal importBasePrice = 0m;
                if (!string.IsNullOrWhiteSpace(agreement.ImportResourceType) &&
                    resources.TryGetValue(agreement.ImportResourceType, out var importRes))
                {
                    importBasePrice = importRes.BasePrice;
                }

                decimal exportPrice = (agreement.ExportUnitPrice > 0m ? agreement.ExportUnitPrice : exportBasePrice) * inflationPriceMultiplier;
                decimal importPrice = (agreement.ImportUnitPrice > 0m ? agreement.ImportUnitPrice : importBasePrice) * inflationPriceMultiplier;

                float exportPartnerModifier = 1f;
                float importPartnerModifier = 1f;
                float exportProductModifier = 1f;
                float importProductModifier = 1f;
                if (agreement.PartnerCountryId.HasValue && partnerSanctions.TryGetValue(agreement.PartnerCountryId.Value, out var partnerSanction))
                {
                    exportPartnerModifier = partnerSanction.ExportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(partnerSanction.ExportEffectiveness);
                    importPartnerModifier = partnerSanction.ImportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(partnerSanction.ImportEffectiveness);
                }

                if (agreement.PartnerCountryId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(agreement.ExportResourceType) &&
                        productSanctionsMap.TryGetValue($"{agreement.PartnerCountryId.Value}|{agreement.ExportResourceType}".ToLowerInvariant(), out var exportProductSanction))
                    {
                        exportProductModifier = exportProductSanction.ExportLevel == (int)SanctionsLevel.Embargo
                            ? 0f
                            : EconomyCalculator.ApplyPercentModifier(exportProductSanction.ExportEffectiveness);
                    }

                    if (!string.IsNullOrWhiteSpace(agreement.ImportResourceType) &&
                        productSanctionsMap.TryGetValue($"{agreement.PartnerCountryId.Value}|{agreement.ImportResourceType}".ToLowerInvariant(), out var importProductSanction))
                    {
                        importProductModifier = importProductSanction.ImportLevel == (int)SanctionsLevel.Embargo
                            ? 0f
                            : EconomyCalculator.ApplyPercentModifier(importProductSanction.ImportEffectiveness);
                    }
                }

                decimal exportLawFactor = exportLawFactorGlobal;
                string exportResourceKey = NormalizeResourceFlowKey(agreement.ExportResourceType);
                if (!string.IsNullOrWhiteSpace(exportResourceKey) &&
                    TryResolveScopedLawFactor(exportLimitByResource, exportResourceKey, resources, out var exportScopedFactor))
                {
                    exportLawFactor = Math.Min(exportLawFactor, ClampDecimal(exportScopedFactor, 0m, 1m));
                }

                decimal importLawFactor = importLawFactorGlobal;
                string importResourceKey = NormalizeResourceFlowKey(agreement.ImportResourceType);
                if (!string.IsNullOrWhiteSpace(importResourceKey) &&
                    TryResolveScopedLawFactor(importLimitByResource, importResourceKey, resources, out var importScopedFactor))
                {
                    importLawFactor = Math.Min(importLawFactor, ClampDecimal(importScopedFactor, 0m, 1m));
                }

                decimal exportPerDayQty = ResolveTradePerDayQuantity(agreement, isExport: true);
                decimal importPerDayQty = ResolveTradePerDayQuantity(agreement, isExport: false);
                decimal exportQty = exportPerDayQty * simulationDays;
                decimal importQty = importPerDayQty * simulationDays;
                exportQty *= (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)exportPartnerModifier * (decimal)exportProductModifier * exportLawFactor;
                importQty *= (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)importPartnerModifier * (decimal)importProductModifier * importLawFactor;

                decimal exportValue = exportQty * exportPrice;
                decimal importValue = importQty * importPrice;

                effects.ExportIncome += exportValue;
                effects.ImportCost += importValue;

                if (exportQty > 0m &&
                    !string.IsNullOrWhiteSpace(agreement.ExportResourceType))
                {
                    AddTradeFlowQuantity(effects.ExportByResource, agreement.ExportResourceType, exportQty);
                }

                if (importQty > 0m &&
                    !string.IsNullOrWhiteSpace(agreement.ImportResourceType))
                {
                    AddTradeFlowQuantity(effects.ImportByResource, agreement.ImportResourceType, importQty);
                }
            }

            return effects;
        }

        private static decimal ResolveTradePerDayQuantity(EconomyTradeAgreementRecord agreement, bool isExport)
        {
            if (agreement == null)
                return 0m;

            decimal rawQty = Math.Max(0m, isExport ? agreement.ExportQty : agreement.ImportQty);
            if (rawQty <= 0m)
                return 0m;

            if (agreement.QuantityIsPerDay)
                return rawQty;

            int deliveryDays = Math.Max(1, agreement.PeriodDays);
            return rawQty / deliveryDays;
        }

        private static void AddTradeFlowQuantity(Dictionary<string, decimal> target, string resourceType, decimal qty)
        {
            if (qty <= 0m)
                return;

            string key = NormalizeResourceFlowKey(resourceType);
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (target.TryGetValue(key, out var current))
                target[key] = current + qty;
            else
                target[key] = qty;
        }

        private static bool TryResolveScopedLawFactor(
            IReadOnlyDictionary<string, decimal> scopedFactors,
            string resourceKey,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources,
            out decimal factor)
        {
            factor = 0m;
            if (scopedFactors == null || scopedFactors.Count == 0 || string.IsNullOrWhiteSpace(resourceKey))
                return false;

            if (scopedFactors.TryGetValue(resourceKey, out factor))
                return true;

            string? trackedResource = TrackedResourceResolver.ResolveTrackedResourceName(resourceKey, resources);
            return !string.IsNullOrWhiteSpace(trackedResource) &&
                   scopedFactors.TryGetValue(trackedResource, out factor);
        }

        private static ActiveLawEconomyPolicy BuildActiveLawEconomyPolicy(int countryId)
        {
            var policy = new ActiveLawEconomyPolicy();
            if (countryId <= 0)
                return policy;

            List<LawRecord> acceptedLaws;
            try
            {
                acceptedLaws = LawsData.GetByCountryAndStatus(countryId, AcceptedLawStatus);
            }
            catch
            {
                return policy;
            }

            if (acceptedLaws == null || acceptedLaws.Count == 0)
                return policy;

            DateTime nowUtc = DateTime.UtcNow;
            bool hasImportLimitGlobal = false;
            bool hasExportLimitGlobal = false;
            bool hasCompanyUseLimit = false;

            foreach (var law in acceptedLaws)
            {
                if (law == null)
                    continue;
                if (law.ExpiresAt.HasValue && law.ExpiresAt.Value.ToUniversalTime() <= nowUtc)
                    continue;

                var effects = ParseLawEffectEntriesForEconomy(law.EffectType);
                foreach (var effect in effects)
                {
                    string baseEffectType = GetEconomyLawEffectBaseType(effect.EffectType);
                    decimal effectValue = effect.EffectValue ?? law.EffectValue;
                    if (string.Equals(baseEffectType, AddTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        policy.TreasuryDailyDelta += Math.Abs(effectValue);
                        continue;
                    }

                    if (string.Equals(baseEffectType, RemoveTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        policy.TreasuryDailyDelta -= Math.Abs(effectValue);
                        continue;
                    }

                    if (string.Equals(baseEffectType, LimitImportEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        decimal factor = NormalizeLawPercentFactor(effectValue);
                        if (TryGetEconomyLawEffectScopeResources(effect.EffectType, out var scopedResources) &&
                            scopedResources.Count > 0)
                        {
                            foreach (var resource in scopedResources)
                            {
                                string resourceKey = NormalizeResourceFlowKey(resource);
                                if (string.IsNullOrWhiteSpace(resourceKey))
                                    continue;

                                if (policy.ImportLimitByResource.TryGetValue(resourceKey, out var currentFactor))
                                    policy.ImportLimitByResource[resourceKey] = Math.Min(currentFactor, factor);
                                else
                                    policy.ImportLimitByResource[resourceKey] = factor;
                            }
                        }
                        else
                        {
                            policy.ImportLimitFactor = hasImportLimitGlobal
                                ? Math.Min(policy.ImportLimitFactor, factor)
                                : factor;
                            hasImportLimitGlobal = true;
                        }
                        continue;
                    }

                    if (string.Equals(baseEffectType, LimitExportEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        decimal factor = NormalizeLawPercentFactor(effectValue);
                        if (TryGetEconomyLawEffectScopeResources(effect.EffectType, out var scopedResources) &&
                            scopedResources.Count > 0)
                        {
                            foreach (var resource in scopedResources)
                            {
                                string resourceKey = NormalizeResourceFlowKey(resource);
                                if (string.IsNullOrWhiteSpace(resourceKey))
                                    continue;

                                if (policy.ExportLimitByResource.TryGetValue(resourceKey, out var currentFactor))
                                    policy.ExportLimitByResource[resourceKey] = Math.Min(currentFactor, factor);
                                else
                                    policy.ExportLimitByResource[resourceKey] = factor;
                            }
                        }
                        else
                        {
                            policy.ExportLimitFactor = hasExportLimitGlobal
                                ? Math.Min(policy.ExportLimitFactor, factor)
                                : factor;
                            hasExportLimitGlobal = true;
                        }
                        continue;
                    }

                    if (string.Equals(baseEffectType, LimitCompanyResourceUseEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        decimal factor = NormalizeLawPercentFactor(effectValue);
                        policy.CompanyResourceUseFactor = hasCompanyUseLimit
                            ? Math.Min(policy.CompanyResourceUseFactor, factor)
                            : factor;
                        hasCompanyUseLimit = true;
                        continue;
                    }

                    if (string.Equals(baseEffectType, SocialContributionEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        policy.SocialContributionRateDelta += NormalizeSignedLawPercentDelta(effectValue);
                        continue;
                    }

                    if (string.Equals(baseEffectType, GrossIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        policy.GrossIncomeTaxRateDelta += NormalizeSignedLawPercentDelta(effectValue);
                        continue;
                    }

                    if (string.Equals(baseEffectType, NetIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        policy.NetIncomeTaxRateDelta += NormalizeSignedLawPercentDelta(effectValue);
                    }
                }
            }

            if (!hasImportLimitGlobal)
                policy.ImportLimitFactor = 1m;
            if (!hasExportLimitGlobal)
                policy.ExportLimitFactor = 1m;
            if (!hasCompanyUseLimit)
                policy.CompanyResourceUseFactor = 1m;
            if (policy.ImportLimitByResource.Count > 0)
                policy.ImportLimitFactor = Math.Min(policy.ImportLimitFactor, policy.ImportLimitByResource.Values.Min());
            if (policy.ExportLimitByResource.Count > 0)
                policy.ExportLimitFactor = Math.Min(policy.ExportLimitFactor, policy.ExportLimitByResource.Values.Min());
            policy.SocialContributionRateDelta = ClampDecimal(policy.SocialContributionRateDelta, -0.95m, 0.95m);
            policy.GrossIncomeTaxRateDelta = ClampDecimal(policy.GrossIncomeTaxRateDelta, -0.95m, 0.95m);
            policy.NetIncomeTaxRateDelta = ClampDecimal(policy.NetIncomeTaxRateDelta, -0.95m, 0.95m);

            return policy;
        }

        private static List<ParsedLawEffect> ParseLawEffectEntriesForEconomy(string effectTypesRaw)
        {
            var entries = new List<ParsedLawEffect>();
            if (string.IsNullOrWhiteSpace(effectTypesRaw))
                return entries;

            var parts = effectTypesRaw.Split(new[] { '|', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawPart in parts)
            {
                string part = (rawPart ?? string.Empty).Trim();
                if (part.Length == 0)
                    continue;

                string typePart = part;
                string valuePart = string.Empty;
                int eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    typePart = part.Substring(0, eqIndex).Trim();
                    valuePart = part.Substring(eqIndex + 1).Trim();
                }
                else
                {
                    int colonIndex = part.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        typePart = part.Substring(0, colonIndex).Trim();
                        valuePart = part.Substring(colonIndex + 1).Trim();
                    }
                    else if (!part.Contains(" ", StringComparison.Ordinal) &&
                             part.Contains(",", StringComparison.Ordinal) &&
                             !part.Contains("[", StringComparison.Ordinal))
                    {
                        foreach (string commaToken in part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string normalizedCommaType = NormalizeEconomyLawEffectType(commaToken);
                            if (!string.IsNullOrWhiteSpace(normalizedCommaType))
                            {
                                entries.Add(new ParsedLawEffect
                                {
                                    EffectType = normalizedCommaType,
                                    EffectValue = null
                                });
                            }
                        }
                        continue;
                    }
                }

                string normalizedType = NormalizeEconomyLawEffectType(typePart);
                if (string.IsNullOrWhiteSpace(normalizedType))
                    continue;

                decimal? parsedValue = null;
                if (!string.IsNullOrWhiteSpace(valuePart) &&
                    decimal.TryParse(valuePart.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
                {
                    parsedValue = numericValue;
                }

                entries.Add(new ParsedLawEffect
                {
                    EffectType = normalizedType,
                    EffectValue = parsedValue
                });
            }

            return entries;
        }

        private static string NormalizeEconomyLawEffectType(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            string input = effectType.Trim();
            if (TrySplitEconomyLawEffectScopeSpecifier(input, out var rawBaseType, out var rawScope))
            {
                string normalizedBase = NormalizeEconomyLawEffectTypeCore(rawBaseType);
                if (string.IsNullOrWhiteSpace(normalizedBase))
                    return string.Empty;
                if (!EconomyLawEffectSupportsResourceScope(normalizedBase))
                    return normalizedBase;

                var resources = ParseEconomyLawEffectResourceTokens(rawScope);
                return BuildScopedEconomyLawEffectType(normalizedBase, resources);
            }

            return NormalizeEconomyLawEffectTypeCore(input);
        }

        private static string NormalizeEconomyLawEffectTypeCore(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            string normalized = effectType.Trim();
            if (string.Equals(normalized, AddTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ДодатиВКазнуЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "КазнаПлюсЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryDailyAdd", StringComparison.OrdinalIgnoreCase))
                return AddTreasuryDailyEffectType;

            if (string.Equals(normalized, RemoveTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СписатиЗКазниЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "КазнаМінусЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryDailyRemove", StringComparison.OrdinalIgnoreCase))
                return RemoveTreasuryDailyEffectType;

            if (string.Equals(normalized, LimitImportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиІмпорт", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітІмпорту", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ImportLimit", StringComparison.OrdinalIgnoreCase))
                return LimitImportEffectType;

            if (string.Equals(normalized, LimitExportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиЕкспорт", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітЕкспорту", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ExportLimit", StringComparison.OrdinalIgnoreCase))
                return LimitExportEffectType;

            if (string.Equals(normalized, LimitCompanyResourceUseEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітСпоживанняКомпаній", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиСпоживанняКомпаній", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "CompanyResourceUseLimit", StringComparison.OrdinalIgnoreCase))
                return LimitCompanyResourceUseEffectType;

            if (string.Equals(normalized, SocialContributionEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Social", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Соціум", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоцВнесок", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоціальніЗбори", StringComparison.OrdinalIgnoreCase))
                return SocialContributionEffectType;

            if (string.Equals(normalized, GrossIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Tax", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Податки", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ПодатокНаБрутто", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "GrossTax", StringComparison.OrdinalIgnoreCase))
                return GrossIncomeTaxEffectType;

            if (string.Equals(normalized, NetIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ПодатокНаНетто", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "NetTax", StringComparison.OrdinalIgnoreCase))
                return NetIncomeTaxEffectType;

            return normalized;
        }

        private static bool EconomyLawEffectSupportsResourceScope(string effectType)
        {
            string baseType = NormalizeEconomyLawEffectTypeCore(effectType);
            return string.Equals(baseType, LimitImportEffectType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseType, LimitExportEffectType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySplitEconomyLawEffectScopeSpecifier(string effectType, out string baseEffectType, out string scopeRaw)
        {
            baseEffectType = string.Empty;
            scopeRaw = string.Empty;

            string input = (effectType ?? string.Empty).Trim();
            if (input.Length < 3 || !input.EndsWith("]", StringComparison.Ordinal))
                return false;

            int openIndex = input.IndexOf('[');
            if (openIndex <= 0 || openIndex >= input.Length - 1)
                return false;

            baseEffectType = input.Substring(0, openIndex).Trim();
            scopeRaw = input.Substring(openIndex + 1, input.Length - openIndex - 2).Trim();
            return !string.IsNullOrWhiteSpace(baseEffectType);
        }

        private static string GetEconomyLawEffectBaseType(string effectType)
        {
            string normalized = NormalizeEconomyLawEffectType(effectType);
            if (TrySplitEconomyLawEffectScopeSpecifier(normalized, out var rawBaseType, out _))
                return NormalizeEconomyLawEffectTypeCore(rawBaseType);
            return NormalizeEconomyLawEffectTypeCore(normalized);
        }

        private static bool TryGetEconomyLawEffectScopeResources(string effectType, out List<string> resources)
        {
            resources = new List<string>();
            string normalized = NormalizeEconomyLawEffectType(effectType);
            if (!TrySplitEconomyLawEffectScopeSpecifier(normalized, out _, out var scopeRaw))
                return false;

            resources = ParseEconomyLawEffectResourceTokens(scopeRaw);
            return resources.Count > 0;
        }

        private static string BuildScopedEconomyLawEffectType(string baseEffectType, IEnumerable<string> resources)
        {
            string baseType = NormalizeEconomyLawEffectTypeCore(baseEffectType);
            if (string.IsNullOrWhiteSpace(baseType))
                return string.Empty;
            if (!EconomyLawEffectSupportsResourceScope(baseType))
                return baseType;

            var parsedResources = ParseEconomyLawEffectResourceTokens(string.Join(",", resources ?? Enumerable.Empty<string>()));
            if (parsedResources.Count == 0)
                return baseType;

            return $"{baseType}[{string.Join(",", parsedResources)}]";
        }

        private static List<string> ParseEconomyLawEffectResourceTokens(string scopeRaw)
        {
            var resources = new List<string>();
            if (string.IsNullOrWhiteSpace(scopeRaw))
                return resources;

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tokens = scopeRaw.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawToken in tokens)
            {
                string token = (rawToken ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;
                if (token == "*" ||
                    token.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("всі", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("усі", StringComparison.OrdinalIgnoreCase))
                {
                    resources.Clear();
                    return resources;
                }

                string normalized = NormalizeEconomyResourceInputKey(token);
                if (unique.Add(normalized))
                    resources.Add(normalized);
            }

            resources.Sort(StringComparer.OrdinalIgnoreCase);
            return resources;
        }

        private static string NormalizeEconomyResourceInputKey(string token)
        {
            string input = (token ?? string.Empty).Trim().ToLowerInvariant();
            return input switch
            {
                "їжа" or "харчі" or "продовольство" => "Food",
                "одяг" => "Clothing",
                "житло" => "Housing",
                "інструменти" or "знаряддя" => "Tools",
                "товари" => "Goods",
                "розкіш" => "Luxury",
                "зброя" => "Weapons",
                "боєприпаси" => "Ammunition",
                "броня" => "Armor",
                "артилерія" => "Artillery",
                "забезпечення" => "Support",
                _ => string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim()
            };
        }

        private static decimal NormalizeLawPercentFactor(decimal value)
        {
            decimal abs = Math.Abs(value);
            decimal factor = abs > 1m ? (abs / 100m) : abs;
            return ClampDecimal(factor, 0m, 1m);
        }

        private static decimal NormalizeSignedLawPercentDelta(decimal value)
        {
            decimal abs = Math.Abs(value);
            decimal factor = abs > 1m ? (abs / 100m) : abs;
            factor = ClampDecimal(factor, 0m, 0.95m);
            return Math.Sign(value) < 0 ? -factor : factor;
        }

        private static decimal NormalizeRateFactor(decimal value)
        {
            decimal abs = Math.Abs(value);
            decimal factor = abs > 1m ? (abs / 100m) : abs;
            factor = ClampDecimal(factor, 0m, 0.95m);
            return Math.Sign(value) < 0 ? -factor : factor;
        }

        private static List<IDemandModifier> BuildDemandModifiers(
            List<EconomyGlobalModifierRecord> globalModifiers,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            var modifiers = new List<IDemandModifier>();
            if (globalModifiers != null && globalModifiers.Count > 0)
            {
                foreach (var mod in globalModifiers)
                {
                    if (Math.Abs(mod.DemandModifier) < 0.0001f)
                        continue;
                    decimal percent = (decimal)mod.DemandModifier;
                    if (string.IsNullOrWhiteSpace(mod.DemandResourceType) || mod.DemandResourceType.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiers.Add(new PercentAllDemandModifier(percent));
                    }
                    else if (Enum.TryParse<ResourceType>(mod.DemandResourceType, true, out var type))
                    {
                        modifiers.Add(new PercentDemandModifier(type, percent));
                    }
                }
            }

            if (resources != null && resources.Count > 0)
            {
                foreach (ResourceType tracked in Enum.GetValues(typeof(ResourceType)))
                {
                    if (!resources.TryGetValue(tracked.ToString(), out var resource))
                        continue;

                    float factorRaw = float.IsNaN(resource.DemandFactor) ? 1f : resource.DemandFactor;
                    float factor = EconomyCalculator.Clamp(factorRaw, 0f, 10f);
                    decimal percent = (decimal)factor - 1m;
                    if (Math.Abs(percent) < 0.0001m)
                        continue;

                    modifiers.Add(new PercentDemandModifier(tracked, percent));
                }
            }

            return modifiers;
        }

        private static Season GetCurrentSeason()
        {
            string raw = EconomyConfigData.GetText("CurrentSeason", "Summer");
            return Enum.TryParse<Season>(raw, true, out var season) ? season : Season.Summer;
        }

        private static float ComputeWealthIndex(CountryRecord country)
        {
            float fallback = EconomyConfigData.GetFloat("DemandWealthDefault", 0.4f);
            if (country.Population <= 0)
                return fallback;

            float scale = EconomyConfigData.GetFloat("DemandWealthPerCapitaScale", 100f);
            if (scale <= 0f)
                return fallback;

            float perCapita = (float)(country.Treasury / (double)country.Population);
            return EconomyCalculator.Clamp(perCapita / scale, 0f, 1f);
        }

        private static bool IsCountryInWealthyState(CountryRecord country)
        {
            if (country == null)
                return false;
            return country.IsInGoldenAge || ParliamentProvisioningService.IsCountryWealthy(country);
        }

        private static void CoverResourceShortagesForWealthyState(ResourceMarketResult market)
        {
            if (market == null)
                return;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var entry = market.GetEntry(type);
                decimal missing = Math.Max(0m, entry.RemainingShortage);
                if (missing <= 0m)
                    continue;

                entry.BlackMarketPurchased += missing;
                entry.Shortage = 0m;
                entry.RemainingShortage = 0m;
                entry.FinalConsumed = entry.Requested;
            }
        }

        private static void ApplyCorruptionAndStability(CountryRecord country, float unemploymentRate, float taxRate, long netResult, EconomyModifiers mods, float globalStabilityModifier)
        {
            float currentCorruption = EconomyCalculator.Clamp(country.Corruption, 0f, 90f);
            float corruption = country.Corruption;
            corruption += unemploymentRate * EconomyConfigData.GetFloat("UnemploymentCorruption", 8f);
            corruption += taxRate * EconomyConfigData.GetFloat("TaxCorruption", 5f);
            if (netResult < 0)
                corruption += EconomyConfigData.GetFloat("DeficitCorruption", 4f);
            if (country.WarState)
                corruption += EconomyConfigData.GetFloat("WarCorruption", 5f);

            float adminReduction = EconomyConfigData.GetFloat("BaseAntiCorruption", 0f);
            corruption -= adminReduction;
            corruption -= mods.CorruptionReduction;
            float targetCorruption = EconomyCalculator.Clamp(corruption, 0f, 90f);
            float corruptionRiseCap = Math.Max(0f, EconomyConfigData.GetFloat("CorruptionRiseCapPerTurn", 1.2f));
            float productionAutoGrowthFactor = (float)GetProductionAutoGrowthFactorForCountry(country.CountryId);
            float corruptionDropCap = Math.Max(0f, EconomyConfigData.GetFloat("CorruptionDropCapPerTurn", 0.8f)) * productionAutoGrowthFactor;
            float corruptionDelta = targetCorruption - currentCorruption;
            float boundedCorruptionDelta = EconomyCalculator.Clamp(corruptionDelta, -corruptionDropCap, corruptionRiseCap);
            country.Corruption = EconomyCalculator.Clamp(currentCorruption + boundedCorruptionDelta, 0f, 90f);

            float currentStability = EconomyCalculator.Clamp(country.Stability, 10f, 100f);
            float stability = country.Stability;
            stability += (1f - unemploymentRate) * EconomyConfigData.GetFloat("EmploymentStability", 6f);
            stability -= unemploymentRate * EconomyConfigData.GetFloat("UnemploymentStability", 8f);
            stability -= taxRate * EconomyConfigData.GetFloat("TaxStabilityPenalty", 4f);
            if (country.WarState)
                stability -= EconomyConfigData.GetFloat("WarStabilityPenalty", 6f);
            if (country.Treasury < 0)
                stability -= EconomyConfigData.GetFloat("DeficitStabilityPenalty", 6f);

            stability += mods.StabilityBonus;
            stability += globalStabilityModifier;
            float targetStability = EconomyCalculator.Clamp(stability, 10f, 100f);
            float stabilityGrowthCap = Math.Max(0f, EconomyConfigData.GetFloat("StabilityGrowthCapPerTurn", 0.9f)) * productionAutoGrowthFactor;
            float stabilityDropCap = Math.Max(0f, EconomyConfigData.GetFloat("StabilityDropCapPerTurn", 1.4f));
            float stabilityDelta = targetStability - currentStability;
            float boundedStabilityDelta = EconomyCalculator.Clamp(stabilityDelta, -stabilityDropCap, stabilityGrowthCap);
            country.Stability = EconomyCalculator.Clamp(currentStability + boundedStabilityDelta, 10f, 100f);
        }

        private static decimal SumBlackMarketPurchased(ResourceMarketResult market)
        {
            decimal total = 0m;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var entry = market.GetEntry(type);
                total += entry.BlackMarketPurchased;
            }
            return total;
        }

        private static decimal SumBlackMarketValue(ResourceMarketResult market)
        {
            decimal total = 0m;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var entry = market.GetEntry(type);
                total += entry.BlackMarketPurchased * entry.BlackMarketPrice;
            }
            return total;
        }

        private static PopulationLifecycleResult UpdatePopulation(CountryRecord country, EconomyModifiers mods, float mortalityRateDelta)
        {
            float growthRate = country.PopulationGrowth + mods.PopulationGrowthBonus;
            if (growthRate < -50f)
                growthRate = -50f;
            return PopulationCalculator.AdvanceLifecycle(country, growthRate, mortalityRateDelta);
        }

        private static bool CheckCrisis(CountryRecord country, float unemploymentRate)
        {
            float stabilityThreshold = EconomyConfigData.GetFloat("CrisisStabilityThreshold", 25f);
            float unemploymentThreshold = EconomyConfigData.GetFloat("CrisisUnemploymentThreshold", 0.35f);
            bool warSanctions = country.WarState && country.SanctionsLevel >= (int)SanctionsLevel.Full;
            return country.Treasury < 0 || country.Stability < stabilityThreshold || unemploymentRate > unemploymentThreshold || warSanctions;
        }

        public List<EconomyWarning> BuildWarnings(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return new List<EconomyWarning>();
            var context = LoadContext(countryId);
            var computation = ComputeTurn(CloneCountry(country), context, previewDays: null, persistBuildings: false);
            return BuildWarnings(countryId, country, computation, context);
        }
        private List<EconomyWarning> BuildWarnings(int countryId, CountryRecord country, EconomyComputation computation, EconomyContext context)
        {
            var warnings = new List<EconomyWarning>();
            var recent = EconomyTurnHistoryData.GetRecentTurns(countryId, 5);

            if (country.Treasury < 0)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.TreasuryNegative,
                    Severity = EconomyWarningSeverity.Critical,
                    Source = EconomyWarningSource.Treasury,
                    Message = "Казна нижче нуля.",
                    SuggestedActions = new List<string> { "Підвищити податки", "Скоротити витрати" }
                });
            }

            int deficitTurns = (int)Math.Round(EconomyConfigData.GetFloat("WarningDeficitTurns", 3f));
            if (recent.Count >= deficitTurns && recent.Take(deficitTurns).All(t => t.NetResult < 0))
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.BudgetDeficitStreak,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Treasury,
                    Message = $"Дефіцит бюджету {FmtNumberCompact(deficitTurns)} ходи поспіль.",
                    SuggestedActions = new List<string> { "Підвищити податки", "Оптимізувати витрати" }
                });
            }

            if (recent.Count >= 2)
            {
                decimal prev = recent[1].Income;
                decimal last = recent[0].Income;
                if (prev > 0)
                {
                    float dropPct = (float)((prev - last) / prev);
                    float threshold = EconomyConfigData.GetFloat("WarningIncomeDropPct", 0.25f);
                    if (dropPct >= threshold)
                    {
                        warnings.Add(new EconomyWarning
                        {
                            WarningType = EconomyWarningType.IncomeDrop,
                            Severity = EconomyWarningSeverity.Warning,
                            Source = EconomyWarningSource.Treasury,
                            Message = $"Різке падіння доходів: -{dropPct * 100f:0.#}%.",
                            SuggestedActions = new List<string> { "Підтримати виробництво", "Активізувати торгівлю" }
                        });
                    }
                }
            }

            float unempThreshold = EconomyConfigData.GetFloat("WarningUnemploymentThreshold", 0.2f);
            if (computation.UnemploymentRate > unempThreshold)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.HighUnemployment,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Безробіття вище {unempThreshold * 100f:0.#}%.",
                    SuggestedActions = new List<string> { "Створити робочі місця", "Запустити держзамовлення" }
                });
            }

            if (computation.SectorDistribution.TotalJobs > computation.SectorDistribution.AvailableWorkers)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.LaborShortage,
                    Severity = EconomyWarningSeverity.Info,
                    Source = EconomyWarningSource.Employment,
                    Message = "Дефіцит робочої сили: місць більше, ніж працівників.",
                    SuggestedActions = new List<string> { "Підвищити частку працездатних", "Оптимізувати склад армії" }
                });
            }

            float stabilityThreshold = EconomyConfigData.GetFloat("WarningStabilityThreshold", 40f);
            if (country.Stability < stabilityThreshold)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.LowStability,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Stability,
                    Message = $"Стабільність нижче порогу {FmtNumberCompact(stabilityThreshold, 1)}.",
                    SuggestedActions = new List<string> { "Підвищити соціальні витрати", "Зменшити податки" }
                });
            }

            if (recent.Count >= 2)
            {
                float stabilityDrop = recent[1].Stability - recent[0].Stability;
                if (stabilityDrop >= 10f)
                {
                    warnings.Add(new EconomyWarning
                    {
                        WarningType = EconomyWarningType.StabilityDrop,
                        Severity = EconomyWarningSeverity.Warning,
                        Source = EconomyWarningSource.Stability,
                        Message = $"Різке падіння стабільності: -{FmtNumberCompact(stabilityDrop, 1)}."
                    });
                }
            }

            if (country.SanctionsLevel > 0)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.ActiveSanctions,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.External,
                    Message = "Активні санкції тиснуть на економіку.",
                    SuggestedActions = new List<string> { "Домовитися про зняття санкцій", "Диверсифікувати торгівлю" }
                });
            }

            float shortageThreshold = EconomyConfigData.GetFloat("WarningShortageScoreThreshold", 30f);
            if (recent.Count > 0 && recent[0].ShortageScore >= shortageThreshold)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.ExternalShock,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.External,
                    Message = $"Високий індекс дефіциту: {FmtNumberCompact(recent[0].ShortageScore, 1)}.",
                    SuggestedActions = new List<string> { "Наростити виробництво", "Зменшити споживання" }
                });
            }

            if (computation.TradeIncome <= 0)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.TradeStopped,
                    Severity = EconomyWarningSeverity.Info,
                    Source = EconomyWarningSource.Trade,
                    Message = "Торгівля зупинена або неактивна.",
                    SuggestedActions = new List<string> { "Підписати нові угоди", "Підвищити конкурентність" }
                });
            }

            if (country.WarState && computation.NetResult < 0)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.WarRecession,
                    Severity = EconomyWarningSeverity.Critical,
                    Source = EconomyWarningSource.External,
                    Message = "Війна + бюджетний дефіцит.",
                    SuggestedActions = new List<string> { "Скоротити неключові витрати", "Збалансувати бюджет" }
                });
            }

            if (context.GlobalModifiers.Count > 0)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.ExternalShock,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.External,
                    Message = "Зовнішні модифікатори погіршують стан економіки.",
                    SuggestedActions = new List<string> { "Підготувати резерви", "Стабілізувати бюджет" }
                });
            }

            if (computation.CrimeLevel >= 60f)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.HighCrime,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.External,
                    Message = $"Тиск злочинності високий: {FmtNumberCompact(computation.CrimeLevel, 1)}%.",
                    SuggestedActions = new List<string> { "Підвищити ефективність поліції", "Знизити корупцію та безробіття" }
                });
            }

            if (computation.CombatEffectiveness <= 45f)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.LowCombatReadiness,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.External,
                    Message = $"Низька бойова готовність: {FmtNumberCompact(computation.CombatEffectiveness, 1)}%.",
                    SuggestedActions = new List<string> { "Підвищити військове забезпечення", "Покращити якість зброї та підготовку" }
                });
            }

            warnings.AddRange(BuildSectorWarnings(computation.SectorDistribution));

            return warnings;
        }

        private static IEnumerable<EconomyWarning> BuildSectorWarnings(SectorDistribution distribution)
        {
            var warnings = new List<EconomyWarning>();
            if (distribution == null || distribution.Entries.Count == 0)
                return warnings;

            float agMin = EconomyConfigData.GetFloat("OptimalAgricultureMin", 0.15f);
            float agMax = EconomyConfigData.GetFloat("OptimalAgricultureMax", 0.25f);
            float indMin = EconomyConfigData.GetFloat("OptimalIndustryMin", 0.25f);
            float indMax = EconomyConfigData.GetFloat("OptimalIndustryMax", 0.35f);
            float servMin = EconomyConfigData.GetFloat("OptimalServicesMin", 0.15f);
            float servMax = EconomyConfigData.GetFloat("OptimalServicesMax", 0.25f);
            float sciMin = EconomyConfigData.GetFloat("OptimalScienceMin", 0.05f);
            float sciMax = EconomyConfigData.GetFloat("OptimalScienceMax", 0.1f);
            float govMilMax = EconomyConfigData.GetFloat("OptimalGovMilMax", 0.25f);

            float ag = distribution.Entries.FirstOrDefault(e => e.Sector == "Agriculture")?.Share ?? 0f;
            float ind = distribution.Entries.FirstOrDefault(e => e.Sector == "Industry")?.Share ?? 0f;
            float serv = distribution.Entries.FirstOrDefault(e => e.Sector == "Services")?.Share ?? 0f;
            float sci = distribution.Entries.FirstOrDefault(e => e.Sector == "Science")?.Share ?? 0f;
            float gov = distribution.Entries.FirstOrDefault(e => e.Sector == "Government")?.Share ?? 0f;
            float mil = distribution.Entries.FirstOrDefault(e => e.Sector == "Military")?.Share ?? 0f;

            if (ag < agMin || ag > agMax)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.SectorImbalance,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Частка агросектору неоптимальна: {ag * 100f:0.#}% (норма {agMin * 100f:0.#}-{agMax * 100f:0.#}%)."
                });
            }
            if (ind < indMin || ind > indMax)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.SectorImbalance,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Частка промисловості неоптимальна: {ind * 100f:0.#}% (норма {indMin * 100f:0.#}-{indMax * 100f:0.#}%)."
                });
            }
            if (serv < servMin || serv > servMax)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.SectorImbalance,
                    Severity = EconomyWarningSeverity.Info,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Частка послуг неоптимальна: {serv * 100f:0.#}% (норма {servMin * 100f:0.#}-{servMax * 100f:0.#}%)."
                });
            }
            if (sci < sciMin || sci > sciMax)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.SectorImbalance,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Частка науки неоптимальна: {sci * 100f:0.#}% (норма {sciMin * 100f:0.#}-{sciMax * 100f:0.#}%)."
                });
            }
            if (gov + mil > govMilMax)
            {
                warnings.Add(new EconomyWarning
                {
                    WarningType = EconomyWarningType.SectorImbalance,
                    Severity = EconomyWarningSeverity.Warning,
                    Source = EconomyWarningSource.Employment,
                    Message = $"Частка держсектору + війська перевищує {govMilMax * 100f:0.#}%."
                });
            }

            return warnings;
        }

        private static int ComputeCrisisRisk(CountryRecord country, float unemploymentRate)
        {
            float stabilityThreshold = EconomyConfigData.GetFloat("CrisisStabilityThreshold", 25f);
            float unemploymentThreshold = EconomyConfigData.GetFloat("CrisisUnemploymentThreshold", 0.35f);
            int risk = 0;

            if (country.Treasury < 0)
                risk = Math.Max(risk, 80);

            if (country.Stability < stabilityThreshold)
                risk = Math.Max(risk, 70);
            else if (country.Stability < stabilityThreshold + 10f)
                risk = Math.Max(risk, 40);

            if (unemploymentRate > unemploymentThreshold)
                risk = Math.Max(risk, 70);
            else if (unemploymentRate > unemploymentThreshold * 0.75f)
                risk = Math.Max(risk, 40);

            if (country.WarState && country.SanctionsLevel >= (int)SanctionsLevel.Full)
                risk = Math.Max(risk, 90);

            return Math.Min(100, risk);
        }

        private static bool HasResourceOutput(EconomyBuildingRecord building)
        {
            if (building == null)
                return false;

            return building.BaseProduction > 0m
                && !string.IsNullOrWhiteSpace(building.ResourceType);
        }

        private static void UpdateFestivals(List<EconomyFestivalRecord> festivals)
        {
            foreach (var f in festivals)
            {
                int remaining = Math.Max(0, f.RemainingTurns - 1);
                bool active = remaining > 0;
                EconomyFestivalsData.DecrementFestival(f.FestivalId, remaining, active);
            }
        }

        private static void PersistPublicSystemsContext(EconomyContext context)
        {
            if (context == null)
                return;

            if (context.PublicSystems != null && context.PublicSystems.CountryId > 0)
                CountryPublicSystemsData.Upsert(context.PublicSystems);

            if (context.Diseases != null)
            {
                foreach (var disease in context.Diseases)
                {
                    if (disease == null || disease.CountryId <= 0 || string.IsNullOrWhiteSpace(disease.Name))
                        continue;
                    CountryDiseasesData.Upsert(disease);
                }
            }

            if (context.IntlPrograms != null)
            {
                foreach (var program in context.IntlPrograms)
                {
                    if (program == null || program.CountryId <= 0 || string.IsNullOrWhiteSpace(program.ProgramType))
                        continue;
                    CountryIntlProgramsData.Upsert(program);
                }
            }
        }

        private static void ApplyScienceProgression(CountryRecord country, decimal scienceSpending, float researchBonus)
        {
            if (country == null || country.CountryId <= 0)
                return;

            decimal progress = Math.Max(0m, EconomyConfigData.GetDecimalForCountry(country.CountryId, ScienceLevelProgressConfigKey, 0m));
            decimal researchProgressBonus = Math.Max(0m, (decimal)researchBonus);
            progress += Math.Max(0m, scienceSpending) + researchProgressBonus;

            int currentLevel = Math.Max(0, country.ResearchLevel);
            int maxLevel = Math.Max(currentLevel, EconomyConfigData.GetInt("ScienceLevelMax", 200));
            decimal baseCost = Math.Max(1m, EconomyConfigData.GetDecimal("ScienceLevelCostBase", 120m));
            decimal growthCost = Math.Max(0m, EconomyConfigData.GetDecimal("ScienceLevelCostGrowth", 35m));
            float speedGainPerLevel = Math.Max(0f, EconomyConfigData.GetFloat("ScienceSpeedPerLevel", 6f));

            int gainedLevels = 0;
            while (currentLevel < maxLevel)
            {
                decimal required = baseCost + growthCost * currentLevel;
                if (progress < required)
                    break;

                progress -= required;
                currentLevel++;
                gainedLevels++;
            }

            if (gainedLevels > 0)
            {
                country.ResearchLevel = currentLevel;
                country.ResearchSpeed = Math.Max(0f, country.ResearchSpeed + gainedLevels * speedGainPerLevel);
            }

            EconomyConfigData.UpsertCountryDecimal(
                country.CountryId,
                ScienceLevelProgressConfigKey,
                decimal.Round(progress, 4, MidpointRounding.AwayFromZero));
        }

        private static void ProgressMilitaryUnits(List<MilitaryUnitRecord> units, SocioMilitarySnapshot mechanics, bool persist)
        {
            if (units == null || units.Count == 0)
                return;

            float quickTrainingPenalty = EconomyConfigData.GetFloat("QuickMobilizationTrainingPenalty", 25f);
            float quickMoralePenalty = EconomyConfigData.GetFloat("QuickMobilizationMoralePenalty", 20f);
            float recoveryBase = EconomyConfigData.GetFloat("MilitaryRecoveryBase", 2.2f);
            float reserveBonus = EconomyConfigData.GetFloat("MilitaryRecoveryReserveBonus", 1.1f);
            float routedPenalty = EconomyConfigData.GetFloat("MilitaryRecoveryRoutedPenalty", 1.0f);
            float fatiguePenaltyScale = EconomyConfigData.GetFloat("MilitaryFatigueTrainingPenaltyScale", 0.35f);
            float supplyCoverage = EconomyCalculator.Clamp(mechanics.SupplyCoverage, 0f, 1f);
            float ammunitionCoverage = EconomyCalculator.Clamp(mechanics.AmmunitionIssued, 0f, 1f);
            float logisticsRecoveryFactor = EconomyCalculator.Clamp(0.45f + supplyCoverage * 0.45f + ammunitionCoverage * 0.25f, 0.2f, 1.4f);
            float moraleTargetBase = EconomyCalculator.Clamp(
                mechanics.CombatEffectiveness * 0.55f + mechanics.CommanderLoyalty * 0.2f + mechanics.PoliceEffectiveness * 0.1f,
                35f,
                78f);

            foreach (var unit in units)
            {
                if (unit == null || Math.Max(0, unit.Soldiers) <= 0)
                    continue;

                string normalizedStatus = (unit.Status ?? string.Empty).Trim();
                if (normalizedStatus.Equals("Destroyed", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isForming = normalizedStatus.Equals("Forming", StringComparison.OrdinalIgnoreCase);
                bool isReserve = normalizedStatus.Equals("Reserve", StringComparison.OrdinalIgnoreCase);
                bool isRouted = normalizedStatus.Equals("Routed", StringComparison.OrdinalIgnoreCase);

                if (isForming)
                {
                    int baseDays = unit.FormationBaseDays > 0
                        ? unit.FormationBaseDays
                        : Math.Max(1, EconomyConfigData.GetInt("MilitaryFormationBaseDays", 7));

                    float formationMultiplier = Math.Max(0.1f, mechanics.FormationTimeMultiplier);
                    float progressPerTurn = unit.IsMobilized
                        ? 1f
                        : 1f / Math.Max(1f, baseDays * formationMultiplier);

                    unit.FormationProgress = EconomyCalculator.Clamp(unit.FormationProgress + progressPerTurn, 0f, 1f);

                    if (unit.IsMobilized)
                    {
                        unit.TrainingLevel = EconomyCalculator.Clamp(unit.TrainingLevel - quickTrainingPenalty, 0f, 100f);
                        unit.Morale = EconomyCalculator.Clamp(unit.Morale - quickMoralePenalty, 0f, 100f);
                        unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness * 0.75f, 0f, 100f);
                        unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel * 0.8f, 0f, 100f);
                    }

                    if (unit.FormationProgress >= 1f)
                    {
                        unit.Status = "Active";
                        unit.IsMobilized = false;
                        unit.FormationProgress = 1f;
                        normalizedStatus = "Active";
                        isForming = false;
                        isReserve = false;
                        isRouted = false;
                    }
                }

                float fatigueRecovery = recoveryBase * logisticsRecoveryFactor;
                if (isReserve)
                    fatigueRecovery += reserveBonus;
                if (isRouted)
                    fatigueRecovery -= routedPenalty;
                if (isForming)
                    fatigueRecovery *= 0.8f;
                fatigueRecovery = EconomyCalculator.Clamp(fatigueRecovery, 0.1f, 6f);

                unit.Fatigue = EconomyCalculator.Clamp(unit.Fatigue - fatigueRecovery, 0f, 100f);

                float moraleTarget = moraleTargetBase;
                if (isReserve)
                    moraleTarget += 4f;
                if (isRouted)
                    moraleTarget -= 8f;
                if (isForming)
                    moraleTarget -= 3f;
                if (unit.IsMobilized)
                    moraleTarget -= 5f;
                moraleTarget = EconomyCalculator.Clamp(moraleTarget, 20f, 90f);

                float moraleDelta = (moraleTarget - unit.Morale) * 0.18f;
                unit.Morale = EconomyCalculator.Clamp(unit.Morale + moraleDelta, 0f, 100f);

                float readinessRegen = EconomyCalculator.Clamp(1.2f + logisticsRecoveryFactor * 2.1f - unit.Fatigue * 0.015f, 0.2f, 4.5f);
                if (isRouted)
                    readinessRegen *= 0.6f;
                if (isForming)
                    readinessRegen *= 0.8f;

                unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness + readinessRegen, 0f, 100f);
                unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel + readinessRegen * 0.95f, 0f, 100f);

                float fatiguePenalty = EconomyCalculator.Clamp(unit.Fatigue / 100f, 0f, 1f);
                float trainingGain = (isForming ? 0.28f : isReserve ? 0.46f : 0.34f)
                    * EconomyCalculator.Clamp(0.65f + mechanics.CombatEffectiveness * 0.005f, 0.4f, 1.4f)
                    * (1f - fatiguePenalty * fatiguePenaltyScale);
                unit.TrainingLevel = EconomyCalculator.Clamp(unit.TrainingLevel + trainingGain, 0f, 100f);

                float experienceGain = (isReserve ? 0.18f : 0.08f) * EconomyCalculator.Clamp(unit.TrainingLevel / 100f, 0.2f, 1f);
                unit.BattleExperience = EconomyCalculator.Clamp(unit.BattleExperience + experienceGain, 0f, 100f);

                if (isRouted && unit.Morale >= 45f && unit.Fatigue <= 35f)
                    unit.Status = "Reserve";

                if (persist)
                    MilitaryUnitsData.UpdateUnit(unit);
            }
        }

        private static void LogTurnMoney(
            int countryId,
            int turnNumber,
            decimal wagesTax,
            decimal profitTax,
            decimal socialContributionIncome,
            decimal additionalGrossIncomeTax,
            decimal additionalNetIncomeTax,
            decimal tradeIncome,
            decimal upkeep,
            decimal govSalaries,
            decimal socialSpending,
            decimal pensionSpending,
            decimal socialProtectedSpending,
            decimal educationSpending,
            decimal unemploymentSupportSpending,
            decimal scienceSpending,
            decimal festivalCost,
            decimal militaryUpkeep,
            decimal militarySalarySpending,
            decimal militarySupplySpending,
            decimal stateOrderBudget,
            decimal infrastructureSpending,
            decimal stabilizationReserveFunding,
            decimal corruptionLoss,
            decimal deputySalaryTotal,
            decimal deputyTheft,
            decimal agreementExportIncome,
            decimal agreementImportCost,
            decimal vassalFunding,
            decimal stateCompanyDividends,
            decimal lawTreasuryDailyIncome,
            decimal lawTreasuryDailyExpense)
        {
            if (wagesTax > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, wagesTax, "Income", "Податок із зарплат", turnNumber);
            if (profitTax > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, profitTax, "Income", "Податок з прибутку", turnNumber);
            if (socialContributionIncome > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, socialContributionIncome, "Income", "Соціальні збори із зарплат", turnNumber);
            if (additionalGrossIncomeTax > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, additionalGrossIncomeTax, "Income", "Додатковий податок на брутто-дохід", turnNumber);
            if (additionalNetIncomeTax > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, additionalNetIncomeTax, "Income", "Додатковий податок на нетто-дохід", turnNumber);
            if (tradeIncome > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, tradeIncome, "Income", "Торгівля", turnNumber);

            if (upkeep > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, upkeep, "Expense", "Утримання будівель", turnNumber);
            if (govSalaries > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, govSalaries, "Expense", "Зарплати держслужбовців", turnNumber);
            if (socialSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, socialSpending, "Expense", "Соціальні виплати", turnNumber);
            if (pensionSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, pensionSpending, "Expense", "Пенсії", turnNumber);
            if (socialProtectedSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, socialProtectedSpending, "Expense", "Соціальний захист", turnNumber);
            if (educationSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, educationSpending, "Expense", "Освіта дітей", turnNumber);
            if (unemploymentSupportSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, unemploymentSupportSpending, "Expense", "Підтримка безробітних", turnNumber);
            if (scienceSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, scienceSpending, "Expense", "Витрати на науку", turnNumber);
            if (festivalCost > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, festivalCost, "Expense", "Витрати на фестивалі", turnNumber);
            if (militaryUpkeep > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, militaryUpkeep, "Expense", "Утримання армії", turnNumber);
            if (militarySalarySpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, militarySalarySpending, "Expense", "Зарплати військовим", turnNumber);
            if (militarySupplySpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, militarySupplySpending, "Expense", "Військове забезпечення", turnNumber);
            if (stateOrderBudget > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, stateOrderBudget, "Expense", "Державні замовлення", turnNumber);
            if (infrastructureSpending > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, infrastructureSpending, "Expense", "Інфраструктура", turnNumber);
            if (stabilizationReserveFunding > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, stabilizationReserveFunding, "Expense", "Стабілізаційне фінансування (інше)", turnNumber);
            if (corruptionLoss > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, corruptionLoss, "Expense", "Втрати від корупції", turnNumber);
            if (deputySalaryTotal > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, deputySalaryTotal, "Expense", "Зарплати депутатів", turnNumber);
            if (deputyTheft > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, deputyTheft, "Expense", "Корупція депутатів", turnNumber);
            if (agreementExportIncome > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, agreementExportIncome, "Income", "Торгівля (угоди: експорт)", turnNumber);
            if (agreementImportCost > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, agreementImportCost, "Expense", "Торгівля (угоди: імпорт)", turnNumber);
            if (vassalFunding > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, vassalFunding, "Income", "Військове фінансування від васалів", turnNumber);
            if (stateCompanyDividends > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, stateCompanyDividends, "Income", "Дивіденди держкомпаній", turnNumber);
            if (lawTreasuryDailyIncome > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, lawTreasuryDailyIncome, "Income", "Закони (щоденне поповнення казни)", turnNumber);
            if (lawTreasuryDailyExpense > 0)
                EconomyMoneyLogData.AddLog(countryId, 0, lawTreasuryDailyExpense, "Expense", "Закони (щоденні витрати казни)", turnNumber);
        }

        private static string BuildSummary(CountryRecord country, EconomyComputation computation, long deficitShortfall)
        {
            string war = country.WarState ? "так" : "ні";
            string sanctions = ((SanctionsLevel)country.SanctionsLevel) switch
            {
                SanctionsLevel.None => "немає",
                SanctionsLevel.Partial => "часткові",
                SanctionsLevel.Full => "повні",
                SanctionsLevel.Embargo => "ембарго",
                _ => "невідомо"
            };
            string crisis = country.IsInCrisis ? "так" : "ні";
            string warning = deficitShortfall > 0
                ? $"\nДефіцит обмежено рівнем казни. Не вистачило: {FmtNumber(deficitShortfall)}."
                : string.Empty;
            string unemploymentLabel = computation.SectorDistribution.TotalJobs == 0
                ? $"{computation.UnemploymentRate * 100f:0.##}% (немає робочих місць)"
                : $"{computation.UnemploymentRate * 100f:0.##}%";

            return
$@"Підсумок економічного ходу
Казна: {FmtNumber(country.Treasury)}
Доходи / Витрати / Баланс: {FmtNumber(computation.Income)} / {FmtNumber(computation.Expenses)} / {FmtNumber(computation.NetResult)}
Закони (казна / день): +{FmtNumber(computation.LawTreasuryDailyIncome)} / -{FmtNumber(computation.LawTreasuryDailyExpense)}
Ліміти торгівлі (імп/експ): {(computation.ImportLimitFactor * 100m):0.##}% / {(computation.ExportLimitFactor * 100m):0.##}%
Компанії ресурси (потрібно/спожито/покриття): {FmtNumber(computation.CompanyResourceRequired)} / {FmtNumber(computation.CompanyResourceConsumed)} / {computation.CompanyResourceCoverage:0.##}%
Компанії (зарплати/дохід/дивіденди): {FmtNumber(computation.CompanyPayroll)} / {FmtNumber(computation.CompanyRevenue)} / {FmtNumber(computation.StateCompanyDividends)}
Ставки доходів (осн/соц/брутто+/нетто+): {(computation.MainIncomeTaxRate * 100m):0.##}% / {(computation.SocialContributionRate * 100m):0.##}% / {(computation.AdditionalGrossIncomeTaxRate * 100m):0.##}% / {(computation.AdditionalNetIncomeTaxRate * 100m):0.##}%
Податки з доходів (осн/соц/брутто+/нетто+): {FmtNumber(computation.WagesTax)} / {FmtNumber(computation.SocialContributionIncome)} / {FmtNumber(computation.AdditionalGrossIncomeTax)} / {FmtNumber(computation.AdditionalNetIncomeTax)}
Доходи громадян (брутто/нетто): {FmtNumber(computation.CitizenGrossIncome)} / {FmtNumber(computation.CitizenNetIncome)}
Інфляція / множник цін: {country.Inflation:0.##}% / x{FmtNumberCompact(GetInflationPriceMultiplier(country.Inflation), 3)}
Безробіття: {unemploymentLabel}
Працездатні / зайняті / безробітні: {FmtNumber(computation.WorkingAgePopulation)} / {FmtNumber(computation.EmployedWorkers)} / {FmtNumber(computation.UnemployedWorkers)}
Ефективність виробництва: {computation.ProductionEfficiency * 100f:0.##}%
Індекс дефіциту: {FmtNumberCompact(computation.ShortageReport.TotalShortageScore)}
Корупційні втрати: {FmtNumber(computation.CorruptionLoss)}
Стабілізаційне фінансування (інше): {FmtNumber(computation.StabilizationReserveFunding)}
Соціальні витрати (пенсії/захист/освіта): {FmtNumber(computation.PensionSpending)} / {FmtNumber(computation.SocialProtectedSpending)} / {FmtNumber(computation.EducationSpending)}
Військові витрати (утримання/зарплати/забезпечення/замовлення): {FmtNumber(computation.MilitaryUpkeep)} / {FmtNumber(computation.MilitarySalarySpending)} / {FmtNumber(computation.MilitarySupplySpending)} / {FmtNumber(computation.StateOrderBudget)}
Готовність (зброя/боєприпаси/боєздатність): {computation.WeaponQuality:0.#}% / {computation.AmmunitionIssued * 100f:0.#}% / {computation.CombatEffectiveness:0.#}%
Злочинність / поліція: {computation.CrimeLevel:0.#}% / {computation.PoliceEffectiveness:0.#}%
МОЗ/МОН/Міжнар. бюджети: {FmtNumber(computation.PublicHealthBudgetSpent)} / {FmtNumber(computation.PublicEducationBudgetSpent)} / {FmtNumber(computation.PublicIntlBudgetSpent)} (+екстр. МОЗ {FmtNumber(computation.PublicEmergencyHealthSpend)}, МОН {FmtNumber(computation.PublicEmergencyEducationSpend)})
МОЗ/МОН/Міжнар. борг бюджету: {FmtNumber(computation.PublicBudgetDebt)}{(computation.PublicBudgetShortfall > 0 ? $" (дефіцит тіку: {FmtNumber(computation.PublicBudgetShortfall)})" : string.Empty)}
МОЗ (Громзд/Сан/Спром/Довіра): {computation.PublicHealthPublic:0.#}/{computation.PublicHealthSanitation:0.#}/{computation.PublicHealthCapacity:0.#}/{computation.PublicHealthTrust:0.#}
МОН (Грам/Якість/Фах/Згурт): {computation.PublicEduLiteracy:0.#}/{computation.PublicEduQuality:0.#}/{computation.PublicEduSpecialists}/{computation.PublicEduCohesion:0.#}
Міжнар. престиж: {computation.PublicIntlPrestige:0.#} | Хвороби: {computation.PublicActiveDiseases} | Потреба в охороні здоров'я: {computation.PublicHealthNeed:0.#} | Множник виробн. (публ.): {computation.PublicProductionMultiplier:0.###}
Події публічних систем: {(string.IsNullOrWhiteSpace(computation.PublicTickNotes) ? "немає" : computation.PublicTickNotes)}
Війна: {war} / Санкції: {sanctions}
Криза: {crisis}{warning}";
        }
        private static CountryRecord CloneCountry(CountryRecord c)
        {
            return new CountryRecord
            {
                CountryId = c.CountryId,
                Name = c.Name,
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
                IncomePerTurn = c.IncomePerTurn,
                ExpensesPerTurn = c.ExpensesPerTurn,
                CorruptionLosses = c.CorruptionLosses,
                WorkRate = c.WorkRate,
                AvailableWorkers = c.AvailableWorkers,
                UnemploymentRate = c.UnemploymentRate,
                WarState = c.WarState,
                SanctionsLevel = c.SanctionsLevel,
                Population = c.Population,
                PopulationGrowth = c.PopulationGrowth,
                Happiness = c.Happiness,
                Unrest = c.Unrest,
                Refugees = c.Refugees,
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
                MilitaryUpkeep = c.MilitaryUpkeep,
                MartialLaw = c.MartialLaw,
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
                LastUpdate = c.LastUpdate,
                TurnsSurvived = c.TurnsSurvived,
                IsInCrisis = c.IsInCrisis,
                IsInGoldenAge = c.IsInGoldenAge,
                IsCollapsed = c.IsCollapsed,
                CreatedAt = c.CreatedAt,
                FlagEmoji = c.FlagEmoji
            };
        }
    }
}










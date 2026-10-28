#nullable enable
using System;
using System.Collections.Generic;

namespace DNDBot.Goverment.Economy
{
    public enum SanctionsLevel
    {
        None = 0,
        Partial = 1,
        Full = 2,
        Embargo = 3
    }

    public enum BuildingCategory
    {
        Housing,
        Production,
        Administration,
        Science,
        Military,
        Culture,
        Other
    }

    public enum BuildingStatus
    {
        UnderConstruction,
        Active,
        Inactive,
        Degraded,
        Abandoned
    }

    public enum EconomyWarningType
    {
        TreasuryNegative,
        BudgetDeficitStreak,
        IncomeDrop,
        HighUnemployment,
        LaborShortage,
        SectorImbalance,
        LowStability,
        StabilityDrop,
        ActiveSanctions,
        TradeStopped,
        WarRecession,
        ExternalShock,
        HighCrime,
        LowCombatReadiness
    }

    public enum EconomyWarningSeverity
    {
        Info,
        Warning,
        Critical
    }

    public enum EconomyWarningSource
    {
        Treasury,
        Employment,
        Trade,
        Stability,
        Corruption,
        External
    }

    public enum CrisisActionType
    {
        SpendingCuts,
        RaiseTaxes,
        EmergencySubsidies,
        AntiCorruptionPurge,
        PrintMoney
    }

    public sealed class EconomyTurnResult
    {
        public int TurnNumber { get; set; }
        public long Income { get; set; }
        public long Expenses { get; set; }
        public long NetResult { get; set; }
        public float CorruptionLoss { get; set; }
        public float EmploymentRate { get; set; }
        public float UnemploymentRate { get; set; }
        public float ProductionEfficiency { get; set; }
        public float ShortageScore { get; set; }
        public float HappinessDelta { get; set; }
        public float MortalityDelta { get; set; }
        public float ProductivityDelta { get; set; }
        public long WorkingAgePopulation { get; set; }
        public long EmployedWorkers { get; set; }
        public long UnemployedWorkers { get; set; }
        public decimal StateOrderBudget { get; set; }
        public float WeaponQuality { get; set; }
        public float AmmunitionIssued { get; set; }
        public float SupplyCoverage { get; set; }
        public float FormationTimeMultiplier { get; set; }
        public float CombatEffectiveness { get; set; }
        public float CrimeLevel { get; set; }
        public bool CrisisTriggered { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class EconomyWarning
    {
        public EconomyWarningType WarningType { get; set; }
        public EconomyWarningSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public EconomyWarningSource Source { get; set; }
        public List<string>? SuggestedActions { get; set; }
    }

    public sealed class BreakdownModifier
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public sealed class BreakdownResult
    {
        public string Name { get; set; } = string.Empty;
        public decimal BaseValue { get; set; }
        public List<BreakdownModifier> Modifiers { get; set; } = new();
        public decimal Total { get; set; }
    }

    public sealed class EconomyPreviewResult
    {
        public int Days { get; set; }
        public long TreasuryNow { get; set; }
        public long TreasuryAfter { get; set; }
        public long Income { get; set; }
        public long Expenses { get; set; }
        public long NetResult { get; set; }
        public long DeficitShortfall { get; set; }

        public decimal WagesTax { get; set; }
        public decimal ProfitTax { get; set; }
        public decimal SocialContributionIncome { get; set; }
        public decimal AdditionalGrossIncomeTax { get; set; }
        public decimal AdditionalNetIncomeTax { get; set; }
        public decimal TradeIncome { get; set; }
        public decimal AgreementExportIncome { get; set; }
        public decimal AgreementImportCost { get; set; }
        public decimal StateCompanyDividends { get; set; }
        public decimal LawTreasuryDailyIncome { get; set; }
        public decimal LawTreasuryDailyExpense { get; set; }
        public decimal CitizenGrossIncome { get; set; }
        public decimal CitizenNetIncome { get; set; }
        public decimal MainIncomeTaxRate { get; set; }
        public decimal SocialContributionRate { get; set; }
        public decimal AdditionalGrossIncomeTaxRate { get; set; }
        public decimal AdditionalNetIncomeTaxRate { get; set; }

        public decimal Upkeep { get; set; }
        public decimal GovSalaries { get; set; }
        public decimal SocialSpending { get; set; }
        public decimal ScienceSpending { get; set; }
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

        public decimal BudgetInfrastructure { get; set; }
        public decimal BudgetMilitary { get; set; }
        public decimal BudgetScience { get; set; }
        public decimal BudgetSocial { get; set; }
        public decimal ImportLimitFactor { get; set; } = 1m;
        public decimal ExportLimitFactor { get; set; } = 1m;
        public decimal CompanyResourceUseFactor { get; set; } = 1m;
        public decimal CompanyResourceRequired { get; set; }
        public decimal CompanyResourceConsumed { get; set; }
        public decimal CompanyResourceCoverage { get; set; } = 100m;

        public float StabilityNow { get; set; }
        public float StabilityAfter { get; set; }
        public float UnemploymentNow { get; set; }
        public float UnemploymentAfter { get; set; }
        public int CrisisRisk { get; set; }
        public List<EconomyWarning> Warnings { get; set; } = new();
    }

    public sealed class SectorDistributionEntry
    {
        public string Sector { get; set; } = string.Empty;
        public long Workers { get; set; }
        public float Share { get; set; }
    }

    public sealed class SectorDistribution
    {
        public long AvailableWorkers { get; set; }
        public long TotalJobs { get; set; }
        public long Unemployed { get; set; }
        public List<SectorDistributionEntry> Entries { get; set; } = new();
    }

    public sealed class CrisisAction
    {
        public CrisisActionType ActionType { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public int CooldownTurns { get; set; }
        public List<string> ImmediateEffects { get; set; } = new();
        public List<string> DelayedEffects { get; set; } = new();
    }

    public sealed class EconomyModifiers
    {
        public float ProductionPenalty { get; set; }
        public float IncomePenalty { get; set; }
        public float StabilityBonus { get; set; }
        public float PopulationGrowthBonus { get; set; }
        public float CorruptionReduction { get; set; }
        public float FestivalBoost { get; set; }
        public float ResearchBonus { get; set; }
        public int ArmyBonus { get; set; }
    }
}


IF OBJECT_ID('EconomyConfig', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyConfig (
        [Key] NVARCHAR(100) NOT NULL PRIMARY KEY,
        ValueDecimal DECIMAL(18,4) NULL,
        ValueText NVARCHAR(200) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WorkRateDefault')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WorkRateDefault', 60);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'TaxEfficiencyBase')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('TaxEfficiencyBase', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BaseWage')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BaseWage', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ProfitTaxFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ProfitTaxFactor', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ExportShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ExportShare', 0.3);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarProductionModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarProductionModifier', 0.85);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsNoneModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsNoneModifier', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsPartialModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsPartialModifier', 0.9);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsFullModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsFullModifier', 0.01);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsExportNoneModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsExportNoneModifier', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsExportPartialModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsExportPartialModifier', 0.9);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsExportFullModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsExportFullModifier', 0.01);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsExportEmbargoModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsExportEmbargoModifier', 0);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsImportNoneModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsImportNoneModifier', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsImportPartialModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsImportPartialModifier', 0.9);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsImportFullModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsImportFullModifier', 0.01);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SanctionsImportEmbargoModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SanctionsImportEmbargoModifier', 0);


UPDATE EconomyConfig SET ValueDecimal = 0.01
WHERE [Key] IN ('SanctionsFullModifier', 'SanctionsExportFullModifier', 'SanctionsImportFullModifier')
  AND ValueDecimal = 0.7;
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarTradeModifier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarTradeModifier', 0.7);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'GovSalaryPerWorker')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('GovSalaryPerWorker', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SocialCostPerUnemployed')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SocialCostPerUnemployed', 0.3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ScienceExpensePerLevel')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ScienceExpensePerLevel', 5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CorruptionLossFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CorruptionLossFactor', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DeputySalaryBase')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DeputySalaryBase', 5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DeputyCorruptionFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DeputyCorruptionFactor', 1);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'UnemploymentCorruption')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('UnemploymentCorruption', 8);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'TaxCorruption')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('TaxCorruption', 5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DeficitCorruption')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DeficitCorruption', 4);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarCorruption')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarCorruption', 5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BaseAntiCorruption')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BaseAntiCorruption', 0);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'EmploymentStability')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('EmploymentStability', 6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'UnemploymentStability')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('UnemploymentStability', 8);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'TaxStabilityPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('TaxStabilityPenalty', 4);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarStabilityPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarStabilityPenalty', 6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DeficitStabilityPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DeficitStabilityPenalty', 6);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrisisStabilityThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrisisStabilityThreshold', 25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrisisUnemploymentThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrisisUnemploymentThreshold', 0.35);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarWorkforceReduction')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarWorkforceReduction', 0.10);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarExtraExpensePerCitizen')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarExtraExpensePerCitizen', 0.02);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarningUnemploymentThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarningUnemploymentThreshold', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarningStabilityThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarningStabilityThreshold', 40);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarningIncomeDropPct')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarningIncomeDropPct', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarningDeficitTurns')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarningDeficitTurns', 3);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BuildingInactiveDegradeTurns')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BuildingInactiveDegradeTurns', 3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BuildingDegradeStepPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BuildingDegradeStepPenalty', 0.1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BuildingMaxDegrade')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BuildingMaxDegrade', 4);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PreviewDaysDefault')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PreviewDaysDefault', 7);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalAgricultureMin')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalAgricultureMin', 0.15);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalAgricultureMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalAgricultureMax', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalIndustryMin')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalIndustryMin', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalIndustryMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalIndustryMax', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalServicesMin')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalServicesMin', 0.15);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalServicesMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalServicesMax', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalScienceMin')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalScienceMin', 0.05);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalScienceMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalScienceMax', 0.1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'OptimalGovMilMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('OptimalGovMilMax', 0.25);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandFoodPerPerson')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandFoodPerPerson', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandClothingPerPerson')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandClothingPerPerson', 0.3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandClothingWinterMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandClothingWinterMultiplier', 1.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandHousingPerNewPerson')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandHousingPerNewPerson', 0.5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandHousingPerPerson')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandHousingPerPerson', 0.1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandToolsPerWorker')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandToolsPerWorker', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandLuxuryPerPerson')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandLuxuryPerPerson', 0.15);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandLuxuryHappinessWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandLuxuryHappinessWeight', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandLuxuryWealthWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandLuxuryWealthWeight', 0.4);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandLuxuryEnabled')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandLuxuryEnabled', 1);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandPopulationChildrenShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandPopulationChildrenShare', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandPopulationWorkingShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandPopulationWorkingShare', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandPopulationElderlyShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandPopulationElderlyShare', 0.15);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandWealthPerCapitaScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandWealthPerCapitaScale', 100);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'DemandWealthDefault')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('DemandWealthDefault', 0.4);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CurrentSeason')
    INSERT INTO EconomyConfig ([Key], ValueText) VALUES ('CurrentSeason', 'Summer');

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketMinFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketMinFactor', 0.3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketMaxFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketMaxFactor', 0.7);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketPriceMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketPriceMultiplier', 1.5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketBaseSuccess')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketBaseSuccess', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketCorruptionWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketCorruptionWeight', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketRiskWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketRiskWeight', 0.3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'BlackMarketRiskDefault')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('BlackMarketRiskDefault', 0.3);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ShortageHappinessScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ShortageHappinessScale', 10);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ShortageMortalityScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ShortageMortalityScale', 0.02);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ShortageProductivityScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ShortageProductivityScale', 0.15);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'ShortageScoreScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('ShortageScoreScale', 100);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarningShortageScoreThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarningShortageScoreThreshold', 30);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PopulationSocialProtectedShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PopulationSocialProtectedShare', 0.05);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PensionPerPensioner')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PensionPerPensioner', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SocialProtectedPayment')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SocialProtectedPayment', 0.45);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'EducationCostPerChild')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('EducationCostPerChild', 0.25);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleChildToWorkingRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleChildToWorkingRate', 0.000152);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleWorkingToPensionRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleWorkingToPensionRate', 0.000058);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleWorkingToSocialRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleWorkingToSocialRate', 0.000014);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecyclePensionToSocialRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecyclePensionToSocialRate', 0.00011);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleChildPressureAgingBoost')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleChildPressureAgingBoost', 0.22);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleDeathRateChildren')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleDeathRateChildren', 0.00001);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleDeathRateWorking')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleDeathRateWorking', 0.000008);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleDeathRatePensioners')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleDeathRatePensioners', 0.00014);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleDeathRateSocial')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleDeathRateSocial', 0.00018);

IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'LifecycleMortalityDeltaScale')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('LifecycleMortalityDeltaScale', 8);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyBaseSalary')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyBaseSalary', 1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyPrivateSalaryMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyPrivateSalaryMultiplier', 1.1);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyStateSalaryMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyStateSalaryMultiplier', 0.95);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyLevelSalaryStep')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyLevelSalaryStep', 0.03);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyTaxBonusPrivate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyTaxBonusPrivate', 0.04);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyTaxBonusState')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyTaxBonusState', -0.03);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyRevenueShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyRevenueShare', 0.82);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyOperationalCostShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyOperationalCostShare', 0.5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyFixedJobCostPerWorker')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyFixedJobCostPerWorker', 0.03);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyBudgetShrinkThresholdDays')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyBudgetShrinkThresholdDays', 1.5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyBudgetGrowthThresholdDays')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyBudgetGrowthThresholdDays', 6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyJobGrowthRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyJobGrowthRate', 0.04);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyJobShrinkRate')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyJobShrinkRate', 0.08);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CompanyMinimumJobs')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CompanyMinimumJobs', 5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateCompanyDividendShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateCompanyDividendShare', 0.5);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderCompanyAllocationShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderCompanyAllocationShare', 0.9);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderTreasuryShare')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderTreasuryShare', 0.02);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderBudgetMax')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderBudgetMax', 10000);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderWarMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderWarMultiplier', 1.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderJobCost')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderJobCost', 25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderProductionMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderProductionMultiplier', 0.45);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'StateOrderWeaponQualityBonus')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('StateOrderWeaponQualityBonus', 0.002);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SoldierSalaryPerTurn')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SoldierSalaryPerTurn', 0.6);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SoldierFoodCostPerTurn')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SoldierFoodCostPerTurn', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SoldierAmmoCostPerTurn')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SoldierAmmoCostPerTurn', 0.4);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SoldierMedicineCostPerTurn')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SoldierMedicineCostPerTurn', 0.12);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WeaponQualityCorruptionPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WeaponQualityCorruptionPenalty', 0.45);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'AmmoCorruptionPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('AmmoCorruptionPenalty', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CombatCorruptionPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CombatCorruptionPenalty', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'MilitaryFormationBaseDays')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('MilitaryFormationBaseDays', 7);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'QuickMobilizationThreshold')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('QuickMobilizationThreshold', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'QuickMobilizationFormationMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('QuickMobilizationFormationMultiplier', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'QuickMobilizationTrainingPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('QuickMobilizationTrainingPenalty', 25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'QuickMobilizationMoralePenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('QuickMobilizationMoralePenalty', 20);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'QuickMobilizationCasualtyMultiplier')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('QuickMobilizationCasualtyMultiplier', 1.4);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'TrainingBaseFromResearch')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('TrainingBaseFromResearch', 20);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'TrainingMobilizationPenalty')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('TrainingMobilizationPenalty', 30);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'SupplyDesertionWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('SupplyDesertionWeight', 0.45);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'MobilizationDesertionWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('MobilizationDesertionWeight', 0.3);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CorruptionDesertionWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CorruptionDesertionWeight', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'WarLossFactor')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('WarLossFactor', 0.015);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PoliceBaseEffectiveness')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PoliceBaseEffectiveness', 25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PoliceControlWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PoliceControlWeight', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'PoliceIntelWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('PoliceIntelWeight', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimeUnemploymentWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimeUnemploymentWeight', 0.35);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimeCorruptionWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimeCorruptionWeight', 0.45);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimeMobilizationWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimeMobilizationWeight', 0.25);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimeSupplyWeight')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimeSupplyWeight', 0.2);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimePoliceSuppression')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimePoliceSuppression', 0.55);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CrimeMartialLawSuppression')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CrimeMartialLawSuppression', 0.7);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'MartialLawExpensePerCitizen')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('MartialLawExpensePerCitizen', 0.02);
IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'MartialLawCoupRiskBonus')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('MartialLawCoupRiskBonus', 4);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'VassalRegimentSize')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('VassalRegimentSize', 2500);


IF NOT EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = 'CurrencyEnabled')
    INSERT INTO EconomyConfig ([Key], ValueDecimal) VALUES ('CurrencyEnabled', 0);

IF OBJECT_ID('CountryPublicSystems', 'U') IS NULL
BEGIN
    CREATE TABLE CountryPublicSystems (
        CountryId INT NOT NULL PRIMARY KEY,
        BudgetHealth DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetHealth DEFAULT (0),
        BudgetEducation DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetEducation DEFAULT (0),
        BudgetIntl DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetIntl DEFAULT (0),
        BudgetIntlMode NVARCHAR(10) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetIntlMode DEFAULT ('Auto'),
        BudgetDebt DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetDebt DEFAULT (0),
        HealthPublic REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthPublic DEFAULT (50),
        HealthSanitation REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthSanitation DEFAULT (50),
        HealthCapacity REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthCapacity DEFAULT (50),
        HealthTrust REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthTrust DEFAULT (50),
        EduLiteracy REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduLiteracy DEFAULT (50),
        EduQuality REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduQuality DEFAULT (50),
        EduSpecialists INT NOT NULL CONSTRAINT DF_CountryPublicSystems_EduSpecialists DEFAULT (50),
        EduCohesion REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduCohesion DEFAULT (50),
        IntlPrestige REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_IntlPrestige DEFAULT (50),
        LastHealthBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_LastHealthBudget DEFAULT (0),
        LastEducationBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_LastEducationBudget DEFAULT (0),
        QuarantineEnabled BIT NOT NULL CONSTRAINT DF_CountryPublicSystems_QuarantineEnabled DEFAULT (0),
        MedicMobilizationTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_MedicMobilizationTicks DEFAULT (0),
        VaccinationProtectionTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_VaccinationProtectionTicks DEFAULT (0),
        HealthDoctors INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDoctors DEFAULT (80),
        HealthDoctorFatigue REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDoctorFatigue DEFAULT (20),
        HealthMedicineStock REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthMedicineStock DEFAULT (35),
        HealthDrugResearchProgress REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugResearchProgress DEFAULT (0),
        HealthDrugLevel INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugLevel DEFAULT (0),
        HealthDrugProjects INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugProjects DEFAULT (0),
        HealthClinicalTrialTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthClinicalTrialTicks DEFAULT (0),
        PendingEmergencyHealthSpend DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_PendingEmergencyHealthSpend DEFAULT (0),
        PendingEmergencyEducationSpend DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_PendingEmergencyEducationSpend DEFAULT (0),
        IntlPrestigeTicker INT NOT NULL CONSTRAINT DF_CountryPublicSystems_IntlPrestigeTicker DEFAULT (0),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryPublicSystems_UpdatedAt DEFAULT (SYSDATETIME())
    );
END;

IF COL_LENGTH('CountryPublicSystems', 'BudgetHealth') IS NULL
    ALTER TABLE CountryPublicSystems ADD BudgetHealth DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetHealth_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'BudgetEducation') IS NULL
    ALTER TABLE CountryPublicSystems ADD BudgetEducation DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetEducation_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'BudgetIntl') IS NULL
    ALTER TABLE CountryPublicSystems ADD BudgetIntl DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetIntl_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'BudgetIntlMode') IS NULL
    ALTER TABLE CountryPublicSystems ADD BudgetIntlMode NVARCHAR(10) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetIntlMode_Mig DEFAULT ('Auto');
IF COL_LENGTH('CountryPublicSystems', 'BudgetDebt') IS NULL
    ALTER TABLE CountryPublicSystems ADD BudgetDebt DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_BudgetDebt_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'HealthPublic') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthPublic REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthPublic_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'HealthSanitation') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthSanitation REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthSanitation_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'HealthCapacity') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthCapacity REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthCapacity_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'HealthTrust') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthTrust REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthTrust_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'EduLiteracy') IS NULL
    ALTER TABLE CountryPublicSystems ADD EduLiteracy REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduLiteracy_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'EduQuality') IS NULL
    ALTER TABLE CountryPublicSystems ADD EduQuality REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduQuality_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'EduSpecialists') IS NULL
    ALTER TABLE CountryPublicSystems ADD EduSpecialists INT NOT NULL CONSTRAINT DF_CountryPublicSystems_EduSpecialists_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'EduCohesion') IS NULL
    ALTER TABLE CountryPublicSystems ADD EduCohesion REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_EduCohesion_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'IntlPrestige') IS NULL
    ALTER TABLE CountryPublicSystems ADD IntlPrestige REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_IntlPrestige_Mig DEFAULT (50);
IF COL_LENGTH('CountryPublicSystems', 'LastHealthBudget') IS NULL
    ALTER TABLE CountryPublicSystems ADD LastHealthBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_LastHealthBudget_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'LastEducationBudget') IS NULL
    ALTER TABLE CountryPublicSystems ADD LastEducationBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_LastEducationBudget_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'QuarantineEnabled') IS NULL
    ALTER TABLE CountryPublicSystems ADD QuarantineEnabled BIT NOT NULL CONSTRAINT DF_CountryPublicSystems_QuarantineEnabled_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'MedicMobilizationTicks') IS NULL
    ALTER TABLE CountryPublicSystems ADD MedicMobilizationTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_MedicMobilizationTicks_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'VaccinationProtectionTicks') IS NULL
    ALTER TABLE CountryPublicSystems ADD VaccinationProtectionTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_VaccinationProtectionTicks_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'HealthDoctors') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthDoctors INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDoctors_Mig DEFAULT (80);
IF COL_LENGTH('CountryPublicSystems', 'HealthDoctorFatigue') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthDoctorFatigue REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDoctorFatigue_Mig DEFAULT (20);
IF COL_LENGTH('CountryPublicSystems', 'HealthMedicineStock') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthMedicineStock REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthMedicineStock_Mig DEFAULT (35);
IF COL_LENGTH('CountryPublicSystems', 'HealthDrugResearchProgress') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthDrugResearchProgress REAL NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugResearchProgress_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'HealthDrugLevel') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthDrugLevel INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugLevel_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'HealthDrugProjects') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthDrugProjects INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthDrugProjects_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'HealthClinicalTrialTicks') IS NULL
    ALTER TABLE CountryPublicSystems ADD HealthClinicalTrialTicks INT NOT NULL CONSTRAINT DF_CountryPublicSystems_HealthClinicalTrialTicks_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'PendingEmergencyHealthSpend') IS NULL
    ALTER TABLE CountryPublicSystems ADD PendingEmergencyHealthSpend DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_PendingEmergencyHealthSpend_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'PendingEmergencyEducationSpend') IS NULL
    ALTER TABLE CountryPublicSystems ADD PendingEmergencyEducationSpend DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryPublicSystems_PendingEmergencyEducationSpend_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'IntlPrestigeTicker') IS NULL
    ALTER TABLE CountryPublicSystems ADD IntlPrestigeTicker INT NOT NULL CONSTRAINT DF_CountryPublicSystems_IntlPrestigeTicker_Mig DEFAULT (0);
IF COL_LENGTH('CountryPublicSystems', 'UpdatedAt') IS NULL
    ALTER TABLE CountryPublicSystems ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryPublicSystems_UpdatedAt_Mig DEFAULT (SYSDATETIME());

IF OBJECT_ID('CountryDiseases', 'U') IS NULL
BEGIN
    CREATE TABLE CountryDiseases (
        DiseaseId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        [Name] NVARCHAR(80) NOT NULL,
        Severity INT NOT NULL CONSTRAINT DF_CountryDiseases_Severity DEFAULT (1),
        Infectiousness INT NOT NULL CONSTRAINT DF_CountryDiseases_Infectiousness DEFAULT (1),
        DurationTicks INT NOT NULL CONSTRAINT DF_CountryDiseases_DurationTicks DEFAULT (1),
        RemainingTicks INT NOT NULL CONSTRAINT DF_CountryDiseases_RemainingTicks DEFAULT (1),
        Stage INT NOT NULL CONSTRAINT DF_CountryDiseases_Stage DEFAULT (1),
        TargetScope NVARCHAR(32) NOT NULL CONSTRAINT DF_CountryDiseases_TargetScope DEFAULT ('whole_state'),
        Active BIT NOT NULL CONSTRAINT DF_CountryDiseases_Active DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryDiseases_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryDiseases_UpdatedAt DEFAULT (SYSDATETIME())
    );
END;

IF COL_LENGTH('CountryDiseases', 'TargetScope') IS NULL
    ALTER TABLE CountryDiseases ADD TargetScope NVARCHAR(32) NOT NULL CONSTRAINT DF_CountryDiseases_TargetScope_Mig DEFAULT ('whole_state');
IF COL_LENGTH('CountryDiseases', 'CreatedAt') IS NULL
    ALTER TABLE CountryDiseases ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryDiseases_CreatedAt_Mig DEFAULT (SYSDATETIME());
IF COL_LENGTH('CountryDiseases', 'UpdatedAt') IS NULL
    ALTER TABLE CountryDiseases ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryDiseases_UpdatedAt_Mig DEFAULT (SYSDATETIME());

IF OBJECT_ID('CountryIntlPrograms', 'U') IS NULL
BEGIN
    CREATE TABLE CountryIntlPrograms (
        IntlProgramId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        ProgramType NVARCHAR(64) NOT NULL,
        PartnerCountryId INT NULL,
        CostPerTick DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryIntlPrograms_CostPerTick DEFAULT (0),
        RequiredSpec INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_RequiredSpec DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_CountryIntlPrograms_IsActive DEFAULT (1),
        TicksActive INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_TicksActive DEFAULT (0),
        SuccessTicks INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_SuccessTicks DEFAULT (0),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryIntlPrograms_UpdatedAt DEFAULT (SYSDATETIME())
    );
END;

IF COL_LENGTH('CountryIntlPrograms', 'PartnerCountryId') IS NULL
    ALTER TABLE CountryIntlPrograms ADD PartnerCountryId INT NULL;
IF COL_LENGTH('CountryIntlPrograms', 'CostPerTick') IS NULL
    ALTER TABLE CountryIntlPrograms ADD CostPerTick DECIMAL(18,2) NOT NULL CONSTRAINT DF_CountryIntlPrograms_CostPerTick_Mig DEFAULT (0);
IF COL_LENGTH('CountryIntlPrograms', 'RequiredSpec') IS NULL
    ALTER TABLE CountryIntlPrograms ADD RequiredSpec INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_RequiredSpec_Mig DEFAULT (0);
IF COL_LENGTH('CountryIntlPrograms', 'IsActive') IS NULL
    ALTER TABLE CountryIntlPrograms ADD IsActive BIT NOT NULL CONSTRAINT DF_CountryIntlPrograms_IsActive_Mig DEFAULT (1);
IF COL_LENGTH('CountryIntlPrograms', 'TicksActive') IS NULL
    ALTER TABLE CountryIntlPrograms ADD TicksActive INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_TicksActive_Mig DEFAULT (0);
IF COL_LENGTH('CountryIntlPrograms', 'SuccessTicks') IS NULL
    ALTER TABLE CountryIntlPrograms ADD SuccessTicks INT NOT NULL CONSTRAINT DF_CountryIntlPrograms_SuccessTicks_Mig DEFAULT (0);
IF COL_LENGTH('CountryIntlPrograms', 'UpdatedAt') IS NULL
    ALTER TABLE CountryIntlPrograms ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CountryIntlPrograms_UpdatedAt_Mig DEFAULT (SYSDATETIME());

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CountryDiseases_Country_Active'
      AND object_id = OBJECT_ID('CountryDiseases')
)
BEGIN
    CREATE INDEX IX_CountryDiseases_Country_Active ON CountryDiseases (CountryId, Active);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CountryIntlPrograms_Country_Active'
      AND object_id = OBJECT_ID('CountryIntlPrograms')
)
BEGIN
    CREATE INDEX IX_CountryIntlPrograms_Country_Active ON CountryIntlPrograms (CountryId, IsActive);
END;

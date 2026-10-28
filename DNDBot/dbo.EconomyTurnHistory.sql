IF OBJECT_ID('EconomyTurnHistory', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyTurnHistory (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        TurnNumber INT NOT NULL,
        Income DECIMAL(28,2) NOT NULL DEFAULT 0,
        Expenses DECIMAL(28,2) NOT NULL DEFAULT 0,
        NetResult DECIMAL(28,2) NOT NULL DEFAULT 0,
        CorruptionLoss DECIMAL(28,2) NOT NULL DEFAULT 0,
        EmploymentRate REAL NOT NULL DEFAULT 0,
        UnemploymentRate REAL NOT NULL DEFAULT 0,
        ProductionValue DECIMAL(28,2) NOT NULL DEFAULT 0,
        TradeIncome DECIMAL(28,2) NOT NULL DEFAULT 0,
        TaxIncome DECIMAL(28,2) NOT NULL DEFAULT 0,
        Stability REAL NOT NULL DEFAULT 0,
        FoodDemand DECIMAL(28,2) NOT NULL DEFAULT 0,
        ClothingDemand DECIMAL(28,2) NOT NULL DEFAULT 0,
        HousingDemand DECIMAL(28,2) NOT NULL DEFAULT 0,
        ToolsDemand DECIMAL(28,2) NOT NULL DEFAULT 0,
        LuxuryDemand DECIMAL(28,2) NOT NULL DEFAULT 0,
        BlackMarketPurchased DECIMAL(28,2) NOT NULL DEFAULT 0,
        BlackMarketValue DECIMAL(28,2) NOT NULL DEFAULT 0,
        ShortageScore REAL NOT NULL DEFAULT 0,
        HappinessDelta REAL NOT NULL DEFAULT 0,
        MortalityDelta REAL NOT NULL DEFAULT 0,
        ProductivityDelta REAL NOT NULL DEFAULT 0,
        PopulationBirths BIGINT NOT NULL DEFAULT 0,
        PopulationChildToWorking BIGINT NOT NULL DEFAULT 0,
        PopulationWorkingToPensioners BIGINT NOT NULL DEFAULT 0,
        PopulationWorkingToSocial BIGINT NOT NULL DEFAULT 0,
        PopulationPensionersToSocial BIGINT NOT NULL DEFAULT 0,
        PopulationDeathsChildren BIGINT NOT NULL DEFAULT 0,
        PopulationDeathsWorking BIGINT NOT NULL DEFAULT 0,
        PopulationDeathsPensioners BIGINT NOT NULL DEFAULT 0,
        PopulationDeathsSocial BIGINT NOT NULL DEFAULT 0,
        PopulationGrowthReduction BIGINT NOT NULL DEFAULT 0,
        PopulationStartChildren BIGINT NOT NULL DEFAULT 0,
        PopulationStartWorkingAge BIGINT NOT NULL DEFAULT 0,
        PopulationStartPensioners BIGINT NOT NULL DEFAULT 0,
        PopulationStartSocialProtected BIGINT NOT NULL DEFAULT 0,
        PopulationEndChildren BIGINT NOT NULL DEFAULT 0,
        PopulationEndWorkingAge BIGINT NOT NULL DEFAULT 0,
        PopulationEndPensioners BIGINT NOT NULL DEFAULT 0,
        PopulationEndSocialProtected BIGINT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;

IF COL_LENGTH('EconomyTurnHistory', 'Stability') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD Stability REAL NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTurnHistory', 'FoodDemand') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD FoodDemand DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'ClothingDemand') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD ClothingDemand DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'HousingDemand') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD HousingDemand DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'ToolsDemand') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD ToolsDemand DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'LuxuryDemand') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD LuxuryDemand DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'BlackMarketPurchased') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD BlackMarketPurchased DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'BlackMarketValue') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD BlackMarketValue DECIMAL(28,2) NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'ShortageScore') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD ShortageScore REAL NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'HappinessDelta') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD HappinessDelta REAL NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'MortalityDelta') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD MortalityDelta REAL NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'ProductivityDelta') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD ProductivityDelta REAL NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTurnHistory', 'PopulationBirths') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationBirths BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationChildToWorking') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationChildToWorking BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationWorkingToPensioners') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationWorkingToPensioners BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationWorkingToSocial') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationWorkingToSocial BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationPensionersToSocial') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationPensionersToSocial BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationDeathsChildren') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationDeathsChildren BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationDeathsWorking') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationDeathsWorking BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationDeathsPensioners') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationDeathsPensioners BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationDeathsSocial') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationDeathsSocial BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationGrowthReduction') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationGrowthReduction BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationStartChildren') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationStartChildren BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationStartWorkingAge') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationStartWorkingAge BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationStartPensioners') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationStartPensioners BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationStartSocialProtected') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationStartSocialProtected BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationEndChildren') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationEndChildren BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationEndWorkingAge') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationEndWorkingAge BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationEndPensioners') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationEndPensioners BIGINT NOT NULL DEFAULT 0;
END;
IF COL_LENGTH('EconomyTurnHistory', 'PopulationEndSocialProtected') IS NULL
BEGIN
    ALTER TABLE EconomyTurnHistory ADD PopulationEndSocialProtected BIGINT NOT NULL DEFAULT 0;
END;


IF OBJECT_ID('EconomyBuildings', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyBuildings (
        BuildingId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        CityName NVARCHAR(60) NOT NULL DEFAULT N'Столиця',
        Type NVARCHAR(50) NOT NULL,
        Category NVARCHAR(30) NOT NULL,
        Level INT NOT NULL DEFAULT 1,
        JobsProvided INT NOT NULL DEFAULT 0,
        UpkeepCost DECIMAL(18,2) NOT NULL DEFAULT 0,
        BaseProduction DECIMAL(18,2) NOT NULL DEFAULT 0,
        BuildCost DECIMAL(18,2) NOT NULL DEFAULT 0,
        BuildTurns INT NOT NULL DEFAULT 0,
        ResourceType NVARCHAR(30) NULL,
        InputResourceType NVARCHAR(30) NULL,
        InputAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        StateOwned BIT NOT NULL DEFAULT 0,
        StabilityBonus REAL NOT NULL DEFAULT 0,
        CorruptionReduction REAL NOT NULL DEFAULT 0,
        ResearchBonus REAL NOT NULL DEFAULT 0,
        PopulationGrowthBonus REAL NOT NULL DEFAULT 0,
        FestivalBoost REAL NOT NULL DEFAULT 0,
        ArmyBonus INT NOT NULL DEFAULT 0,
        CompanyId INT NULL,
        BuildingStatus NVARCHAR(20) NOT NULL DEFAULT 'Active',
        InactiveTurns INT NOT NULL DEFAULT 0,
        DegradationLevel INT NOT NULL DEFAULT 0,
        IgnoreProductionEfficiency BIT NOT NULL DEFAULT 0
    );
END;

IF COL_LENGTH('EconomyBuildings', 'BuildCost') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD BuildCost DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyBuildings', 'CityName') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD CityName NVARCHAR(60) NOT NULL DEFAULT N'Столиця';
END;

IF COL_LENGTH('EconomyBuildings', 'BuildTurns') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD BuildTurns INT NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyBuildings', 'BuildingStatus') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD BuildingStatus NVARCHAR(20) NOT NULL DEFAULT 'Active';
END;

IF COL_LENGTH('EconomyBuildings', 'InactiveTurns') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD InactiveTurns INT NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyBuildings', 'DegradationLevel') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD DegradationLevel INT NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyBuildings', 'IgnoreProductionEfficiency') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD IgnoreProductionEfficiency BIT NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyBuildings', 'CompanyId') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD CompanyId INT NULL;
END;

IF COL_LENGTH('EconomyBuildings', 'InputResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD InputResourceType NVARCHAR(30) NULL;
END;

IF COL_LENGTH('EconomyBuildings', 'InputAmount') IS NULL
BEGIN
    ALTER TABLE EconomyBuildings ADD InputAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyBuildings')
      AND name = 'IX_EconomyBuildings_CountryId'
)
    CREATE INDEX IX_EconomyBuildings_CountryId ON EconomyBuildings (CountryId);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyBuildings')
      AND name = 'IX_EconomyBuildings_CompanyId'
)
    CREATE INDEX IX_EconomyBuildings_CompanyId ON EconomyBuildings (CompanyId);

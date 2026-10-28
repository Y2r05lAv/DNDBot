IF OBJECT_ID('EconomyFestivals', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyFestivals (
        FestivalId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        Name NVARCHAR(80) NOT NULL,
        DurationTurns INT NOT NULL DEFAULT 1,
        RemainingTurns INT NOT NULL DEFAULT 1,
        Cost DECIMAL(18,2) NOT NULL DEFAULT 0,
        StabilityBonus REAL NOT NULL DEFAULT 0,
        PopulationGrowthBonus REAL NOT NULL DEFAULT 0,
        ProductionPenalty REAL NOT NULL DEFAULT 0,
        IncomePenalty REAL NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
END;

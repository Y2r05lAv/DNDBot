IF OBJECT_ID('EconomyGlobalModifiers', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyGlobalModifiers (
        ModifierId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(60) NOT NULL,
        ModifierType NVARCHAR(30) NOT NULL,
        StartAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        DurationDays INT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        ProductionModifier REAL NOT NULL DEFAULT 0,
        TradeModifier REAL NOT NULL DEFAULT 0,
        StabilityModifier REAL NOT NULL DEFAULT 0,
        IncomeModifier REAL NOT NULL DEFAULT 0,
        UnemploymentModifier REAL NOT NULL DEFAULT 0,
        DemandModifier REAL NOT NULL DEFAULT 0,
        DemandResourceType NVARCHAR(30) NULL
    );
END;

IF COL_LENGTH('EconomyGlobalModifiers', 'DemandModifier') IS NULL
BEGIN
    ALTER TABLE EconomyGlobalModifiers ADD DemandModifier REAL NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyGlobalModifiers', 'DemandResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyGlobalModifiers ADD DemandResourceType NVARCHAR(30) NULL;
END;

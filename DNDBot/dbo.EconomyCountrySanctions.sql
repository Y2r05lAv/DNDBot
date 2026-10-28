IF OBJECT_ID('EconomyCountrySanctions', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCountrySanctions (
        SanctionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SourceCountryId INT NOT NULL,
        TargetCountryId INT NOT NULL,
        ExportLevel INT NOT NULL DEFAULT 0,
        ImportLevel INT NOT NULL DEFAULT 0,
        ExportEffectiveness REAL NOT NULL DEFAULT 1,
        ImportEffectiveness REAL NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );

    CREATE UNIQUE INDEX UX_EconomyCountrySanctions_SourceTarget
    ON EconomyCountrySanctions (SourceCountryId, TargetCountryId);
END
GO

IF COL_LENGTH('EconomyCountrySanctions', 'ExportLevel') IS NULL
    ALTER TABLE EconomyCountrySanctions ADD ExportLevel INT NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCountrySanctions', 'ImportLevel') IS NULL
    ALTER TABLE EconomyCountrySanctions ADD ImportLevel INT NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCountrySanctions', 'ExportEffectiveness') IS NULL
    ALTER TABLE EconomyCountrySanctions ADD ExportEffectiveness REAL NOT NULL DEFAULT 1;
IF COL_LENGTH('EconomyCountrySanctions', 'ImportEffectiveness') IS NULL
    ALTER TABLE EconomyCountrySanctions ADD ImportEffectiveness REAL NOT NULL DEFAULT 1;
GO

IF COL_LENGTH('EconomyCountrySanctions', 'Level') IS NOT NULL
BEGIN
    EXEC('UPDATE EconomyCountrySanctions SET ExportLevel = Level, ImportLevel = Level;');
END
GO

IF COL_LENGTH('EconomyCountrySanctions', 'Effectiveness') IS NOT NULL
BEGIN
    EXEC('UPDATE EconomyCountrySanctions SET ExportEffectiveness = Effectiveness, ImportEffectiveness = Effectiveness;');
END
GO

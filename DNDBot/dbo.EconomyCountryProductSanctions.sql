IF OBJECT_ID('EconomyCountryProductSanctions', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCountryProductSanctions (
        SanctionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SourceCountryId INT NOT NULL,
        TargetCountryId INT NOT NULL,
        ResourceType NVARCHAR(30) NOT NULL,
        ExportLevel INT NOT NULL DEFAULT 0,
        ImportLevel INT NOT NULL DEFAULT 0,
        ExportEffectiveness REAL NOT NULL DEFAULT 0,
        ImportEffectiveness REAL NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );

    CREATE UNIQUE INDEX UX_EconomyCountryProductSanctions_SourceTargetResource
    ON EconomyCountryProductSanctions (SourceCountryId, TargetCountryId, ResourceType);
END
GO

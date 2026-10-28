IF OBJECT_ID('dbo.EconomyCities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EconomyCities
    (
        CityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EconomyCities PRIMARY KEY,
        CountryId INT NOT NULL,
        CityName NVARCHAR(60) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_EconomyCities_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.EconomyCities', 'U') IS NOT NULL
BEGIN
    ;WITH Duplicates AS
    (
        SELECT CityId,
               ROW_NUMBER() OVER (PARTITION BY CountryId, CityName ORDER BY CityId) AS rn
        FROM dbo.EconomyCities
    )
    DELETE FROM Duplicates WHERE rn > 1;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.EconomyCities')
          AND name = 'UX_EconomyCities_Country_CityName'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_EconomyCities_Country_CityName
            ON dbo.EconomyCities (CountryId, CityName);
    END

    IF OBJECT_ID('dbo.Countries', 'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.EconomyCities (CountryId, CityName)
        SELECT c.CountryId, N'Столиця'
        FROM dbo.Countries c
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.EconomyCities ec
            WHERE ec.CountryId = c.CountryId
              AND ec.CityName = N'Столиця'
        );
    END

    IF OBJECT_ID('dbo.EconomyBuildings', 'U') IS NOT NULL
       AND COL_LENGTH('dbo.EconomyBuildings', 'CityName') IS NOT NULL
    BEGIN
        INSERT INTO dbo.EconomyCities (CountryId, CityName)
        SELECT DISTINCT
            eb.CountryId,
            LEFT(LTRIM(RTRIM(eb.CityName)), 60)
        FROM dbo.EconomyBuildings eb
        WHERE eb.CountryId > 0
          AND LTRIM(RTRIM(ISNULL(eb.CityName, N''))) <> N''
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.EconomyCities ec
              WHERE ec.CountryId = eb.CountryId
                AND ec.CityName = LEFT(LTRIM(RTRIM(eb.CityName)), 60)
          );
    END
END
GO

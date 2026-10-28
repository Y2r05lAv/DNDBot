IF OBJECT_ID('EconomyCurrencies', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCurrencies (
        CountryId INT NOT NULL PRIMARY KEY,
        CurrencyCode NVARCHAR(16) NOT NULL,
        CurrencyName NVARCHAR(50) NOT NULL,
        BaseRate DECIMAL(18,6) NOT NULL DEFAULT 1,
        CurrentRate DECIMAL(18,6) NOT NULL DEFAULT 1,
        InflationFactor REAL NOT NULL DEFAULT 0,
        SupplyIndex REAL NOT NULL DEFAULT 1,
        DemandIndex REAL NOT NULL DEFAULT 1,
        CentralBankReserve DECIMAL(18,2) NOT NULL DEFAULT 0,
        InterventionStrength REAL NOT NULL DEFAULT 0,
        ExternalShock REAL NOT NULL DEFAULT 0,
        ScarcityIndex REAL NOT NULL DEFAULT 0,
        BlackMarketMultiplier REAL NOT NULL DEFAULT 1.1,
        BlackMarketRate DECIMAL(18,6) NOT NULL DEFAULT 1,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;

IF COL_LENGTH('EconomyCurrencies', 'BlackMarketRate') IS NULL
BEGIN
    ALTER TABLE EconomyCurrencies ADD BlackMarketRate DECIMAL(18,6) NOT NULL DEFAULT 1;
END;

IF OBJECT_ID('EconomyBudget', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyBudget (
        CountryId INT NOT NULL PRIMARY KEY,
        InfrastructureShare DECIMAL(5,4) NOT NULL DEFAULT 0.25,
        MilitaryShare DECIMAL(5,4) NOT NULL DEFAULT 0.25,
        ScienceShare DECIMAL(5,4) NOT NULL DEFAULT 0.25,
        SocialShare DECIMAL(5,4) NOT NULL DEFAULT 0.25,
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;

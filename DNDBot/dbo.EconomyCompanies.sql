IF OBJECT_ID('EconomyCompanies', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCompanies (
        CompanyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        OwnerName NVARCHAR(100) NULL,
        ProductionType NVARCHAR(50) NOT NULL DEFAULT N'General',
        JobsOffered INT NOT NULL DEFAULT 0,
        BaseSalary DECIMAL(18,4) NOT NULL DEFAULT 1,
        TaxRate DECIMAL(5,4) NOT NULL DEFAULT 0.20,
        Budget DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsStateCompany BIT NOT NULL DEFAULT 0,
        MilitaryProductionShare REAL NOT NULL DEFAULT 0,
        InputResourceType NVARCHAR(30) NULL,
        InputAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('EconomyCompanies', 'CountryId') IS NULL
    ALTER TABLE EconomyCompanies ADD CountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'Name') IS NULL
    ALTER TABLE EconomyCompanies ADD Name NVARCHAR(100) NOT NULL DEFAULT N'Company';
IF COL_LENGTH('EconomyCompanies', 'OwnerName') IS NULL
    ALTER TABLE EconomyCompanies ADD OwnerName NVARCHAR(100) NULL;
IF COL_LENGTH('EconomyCompanies', 'ProductionType') IS NULL
    ALTER TABLE EconomyCompanies ADD ProductionType NVARCHAR(50) NOT NULL DEFAULT N'General';
IF COL_LENGTH('EconomyCompanies', 'JobsOffered') IS NULL
    ALTER TABLE EconomyCompanies ADD JobsOffered INT NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'BaseSalary') IS NULL
    ALTER TABLE EconomyCompanies ADD BaseSalary DECIMAL(18,4) NOT NULL DEFAULT 1;
IF COL_LENGTH('EconomyCompanies', 'TaxRate') IS NULL
    ALTER TABLE EconomyCompanies ADD TaxRate DECIMAL(5,4) NOT NULL DEFAULT 0.20;
IF COL_LENGTH('EconomyCompanies', 'Budget') IS NULL
    ALTER TABLE EconomyCompanies ADD Budget DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'IsStateCompany') IS NULL
    ALTER TABLE EconomyCompanies ADD IsStateCompany BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'MilitaryProductionShare') IS NULL
    ALTER TABLE EconomyCompanies ADD MilitaryProductionShare REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'InputResourceType') IS NULL
    ALTER TABLE EconomyCompanies ADD InputResourceType NVARCHAR(30) NULL;
IF COL_LENGTH('EconomyCompanies', 'InputAmount') IS NULL
    ALTER TABLE EconomyCompanies ADD InputAmount DECIMAL(18,4) NOT NULL DEFAULT 0;
IF COL_LENGTH('EconomyCompanies', 'IsActive') IS NULL
    ALTER TABLE EconomyCompanies ADD IsActive BIT NOT NULL DEFAULT 1;
IF COL_LENGTH('EconomyCompanies', 'CreatedAt') IS NULL
    ALTER TABLE EconomyCompanies ADD CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('EconomyCompanies', 'UpdatedAt') IS NULL
    ALTER TABLE EconomyCompanies ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyCompanies')
      AND name = 'IX_EconomyCompanies_CountryId'
)
    CREATE INDEX IX_EconomyCompanies_CountryId ON EconomyCompanies (CountryId);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyCompanies')
      AND name = 'IX_EconomyCompanies_Name'
)
    CREATE INDEX IX_EconomyCompanies_Name ON EconomyCompanies (Name);

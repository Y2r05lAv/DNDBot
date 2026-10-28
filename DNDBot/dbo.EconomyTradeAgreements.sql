IF OBJECT_ID('EconomyTradeAgreements', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyTradeAgreements (
        AgreementId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        PartnerCountryId INT NULL,
        PartnerName NVARCHAR(80) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        ExportBonus REAL NOT NULL DEFAULT 0,
        ImportBonus REAL NOT NULL DEFAULT 0,
        ResourceType NVARCHAR(30) NULL,
        ExportResourceType NVARCHAR(30) NULL,
        ImportResourceType NVARCHAR(30) NULL,
        ExportQty DECIMAL(18,2) NOT NULL DEFAULT 0,
        ExportUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        ImportQty DECIMAL(18,2) NOT NULL DEFAULT 0,
        ImportUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        PeriodDays INT NOT NULL DEFAULT 1,
        DeliveryMode NVARCHAR(30) NOT NULL DEFAULT N'Caravan',
        QuantityIsPerDay BIT NOT NULL DEFAULT 1
    );
END;

IF COL_LENGTH('EconomyTradeAgreements', 'PartnerCountryId') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD PartnerCountryId INT NULL;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ResourceType NVARCHAR(30) NULL;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ExportResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ExportResourceType NVARCHAR(30) NULL;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ImportResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ImportResourceType NVARCHAR(30) NULL;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ExportQty') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ExportQty DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ExportUnitPrice') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ExportUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ImportQty') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ImportQty DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'ImportUnitPrice') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD ImportUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'PeriodDays') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD PeriodDays INT NOT NULL DEFAULT 1;
END;

IF COL_LENGTH('EconomyTradeAgreements', 'DeliveryMode') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD DeliveryMode NVARCHAR(30) NOT NULL DEFAULT N'Caravan';
END;

IF COL_LENGTH('EconomyTradeAgreements', 'QuantityIsPerDay') IS NULL
BEGIN
    ALTER TABLE EconomyTradeAgreements ADD QuantityIsPerDay BIT NOT NULL DEFAULT 1;
END;

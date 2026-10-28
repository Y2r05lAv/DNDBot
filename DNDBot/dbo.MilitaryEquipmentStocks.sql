IF OBJECT_ID('MilitaryEquipmentStocks', 'U') IS NULL
BEGIN
    CREATE TABLE MilitaryEquipmentStocks (
        CountryId INT NOT NULL,
        ResourceType NVARCHAR(30) NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL DEFAULT 0,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MilitaryEquipmentStocks PRIMARY KEY (CountryId, ResourceType)
    );
END;

IF COL_LENGTH('MilitaryEquipmentStocks', 'CountryId') IS NULL
BEGIN
    ALTER TABLE MilitaryEquipmentStocks ADD CountryId INT NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('MilitaryEquipmentStocks', 'ResourceType') IS NULL
BEGIN
    ALTER TABLE MilitaryEquipmentStocks ADD ResourceType NVARCHAR(30) NOT NULL DEFAULT N'';
END;

IF COL_LENGTH('MilitaryEquipmentStocks', 'Quantity') IS NULL
BEGIN
    ALTER TABLE MilitaryEquipmentStocks ADD Quantity DECIMAL(18,2) NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('MilitaryEquipmentStocks', 'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE MilitaryEquipmentStocks ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryEquipmentStocks')
      AND name = 'IX_MilitaryEquipmentStocks_CountryId'
)
BEGIN
    CREATE INDEX IX_MilitaryEquipmentStocks_CountryId
        ON MilitaryEquipmentStocks (CountryId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryEquipmentStocks')
      AND name = 'IX_MilitaryEquipmentStocks_ResourceType'
)
BEGIN
    CREATE INDEX IX_MilitaryEquipmentStocks_ResourceType
        ON MilitaryEquipmentStocks (ResourceType);
END;

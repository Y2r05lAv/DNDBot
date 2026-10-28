IF OBJECT_ID('EconomyResources', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyResources (
        ResourceType NVARCHAR(30) NOT NULL PRIMARY KEY,
        BasePrice DECIMAL(18,2) NOT NULL DEFAULT 1,
        IsTradeable BIT NOT NULL DEFAULT 1,
        DemandFactor REAL NOT NULL DEFAULT 1,
        ParentResourceType NVARCHAR(30) NULL,
        IgnoreProductionEfficiency BIT NOT NULL DEFAULT 0
    );
END;
GO

IF COL_LENGTH('EconomyResources', 'ParentResourceType') IS NULL
BEGIN
    ALTER TABLE EconomyResources
        ADD ParentResourceType NVARCHAR(30) NULL;
END;
GO

IF COL_LENGTH('EconomyResources', 'IgnoreProductionEfficiency') IS NULL
BEGIN
    ALTER TABLE EconomyResources
        ADD IgnoreProductionEfficiency BIT NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_EconomyResources_ParentResourceType'
)
BEGIN
    ALTER TABLE EconomyResources WITH CHECK
        ADD CONSTRAINT FK_EconomyResources_ParentResourceType
        FOREIGN KEY (ParentResourceType) REFERENCES EconomyResources(ResourceType);
END;
GO

IF OBJECT_ID('CK_EconomyResources_ParentNotSelf', 'C') IS NULL
BEGIN
    ALTER TABLE EconomyResources
        ADD CONSTRAINT CK_EconomyResources_ParentNotSelf
        CHECK (ParentResourceType IS NULL OR ParentResourceType <> ResourceType);
END;
GO

IF NOT EXISTS (SELECT 1 FROM EconomyResources WHERE ResourceType = 'Food')
    INSERT INTO EconomyResources (ResourceType, BasePrice, IsTradeable, DemandFactor)
    VALUES ('Food', 1.0, 1, 1.1);
IF NOT EXISTS (SELECT 1 FROM EconomyResources WHERE ResourceType = 'Goods')
    INSERT INTO EconomyResources (ResourceType, BasePrice, IsTradeable, DemandFactor)
    VALUES ('Goods', 2.0, 1, 1.0);
IF NOT EXISTS (SELECT 1 FROM EconomyResources WHERE ResourceType = 'Materials')
    INSERT INTO EconomyResources (ResourceType, BasePrice, IsTradeable, DemandFactor)
    VALUES ('Materials', 1.5, 1, 0.9);

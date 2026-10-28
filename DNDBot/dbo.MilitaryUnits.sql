IF OBJECT_ID('MilitaryUnits', 'U') IS NULL
BEGIN
    CREATE TABLE MilitaryUnits (
        UnitId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        ParentUnitId INT NULL,
        UnitName NVARCHAR(100) NOT NULL DEFAULT N'Частина',
        UnitLevel NVARCHAR(20) NOT NULL DEFAULT 'Squad',
        UnitRole NVARCHAR(20) NOT NULL DEFAULT 'Field',
        UnitType NVARCHAR(20) NOT NULL DEFAULT 'Infantry',
        Race NVARCHAR(40) NOT NULL DEFAULT 'Human',
        Soldiers INT NOT NULL DEFAULT 10,
        EquipmentTier NVARCHAR(20) NOT NULL DEFAULT 'Standard',
        EquipmentQuality REAL NOT NULL DEFAULT 50,
        BattleExperience REAL NOT NULL DEFAULT 15,
        Fatigue REAL NOT NULL DEFAULT 0,
        TrainingLevel REAL NOT NULL DEFAULT 30,
        Morale REAL NOT NULL DEFAULT 50,
        WeaponReadiness REAL NOT NULL DEFAULT 50,
        SupplyLevel REAL NOT NULL DEFAULT 50,
        CommanderName NVARCHAR(100) NULL,
        CommanderSkill REAL NOT NULL DEFAULT 50,
        CommanderLoyalty REAL NOT NULL DEFAULT 50,
        CommanderCorruption REAL NOT NULL DEFAULT 0,
        CommanderExperience REAL NOT NULL DEFAULT 20,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Forming',
        IsMobilized BIT NOT NULL DEFAULT 0,
        FormationProgress REAL NOT NULL DEFAULT 0,
        FormationBaseDays INT NOT NULL DEFAULT 7,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('MilitaryUnits', 'ParentUnitId') IS NULL
    ALTER TABLE MilitaryUnits ADD ParentUnitId INT NULL;
IF COL_LENGTH('MilitaryUnits', 'UnitName') IS NULL
    ALTER TABLE MilitaryUnits ADD UnitName NVARCHAR(100) NOT NULL DEFAULT N'Частина';
IF COL_LENGTH('MilitaryUnits', 'UnitLevel') IS NULL
    ALTER TABLE MilitaryUnits ADD UnitLevel NVARCHAR(20) NOT NULL DEFAULT 'Squad';
IF COL_LENGTH('MilitaryUnits', 'UnitRole') IS NULL
    ALTER TABLE MilitaryUnits ADD UnitRole NVARCHAR(20) NOT NULL DEFAULT 'Field';
IF COL_LENGTH('MilitaryUnits', 'UnitType') IS NULL
    ALTER TABLE MilitaryUnits ADD UnitType NVARCHAR(20) NOT NULL DEFAULT 'Infantry';
IF COL_LENGTH('MilitaryUnits', 'Race') IS NULL
    ALTER TABLE MilitaryUnits ADD Race NVARCHAR(40) NOT NULL DEFAULT 'Human';
IF COL_LENGTH('MilitaryUnits', 'Soldiers') IS NULL
    ALTER TABLE MilitaryUnits ADD Soldiers INT NOT NULL DEFAULT 10;
IF COL_LENGTH('MilitaryUnits', 'EquipmentTier') IS NULL
    ALTER TABLE MilitaryUnits ADD EquipmentTier NVARCHAR(20) NOT NULL DEFAULT 'Standard';
IF COL_LENGTH('MilitaryUnits', 'EquipmentQuality') IS NULL
    ALTER TABLE MilitaryUnits ADD EquipmentQuality REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'BattleExperience') IS NULL
    ALTER TABLE MilitaryUnits ADD BattleExperience REAL NOT NULL DEFAULT 15;
IF COL_LENGTH('MilitaryUnits', 'Fatigue') IS NULL
    ALTER TABLE MilitaryUnits ADD Fatigue REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryUnits', 'TrainingLevel') IS NULL
    ALTER TABLE MilitaryUnits ADD TrainingLevel REAL NOT NULL DEFAULT 30;
IF COL_LENGTH('MilitaryUnits', 'Morale') IS NULL
    ALTER TABLE MilitaryUnits ADD Morale REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'WeaponReadiness') IS NULL
    ALTER TABLE MilitaryUnits ADD WeaponReadiness REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'SupplyLevel') IS NULL
    ALTER TABLE MilitaryUnits ADD SupplyLevel REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'CommanderName') IS NULL
    ALTER TABLE MilitaryUnits ADD CommanderName NVARCHAR(100) NULL;
IF COL_LENGTH('MilitaryUnits', 'CommanderSkill') IS NULL
    ALTER TABLE MilitaryUnits ADD CommanderSkill REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'CommanderLoyalty') IS NULL
    ALTER TABLE MilitaryUnits ADD CommanderLoyalty REAL NOT NULL DEFAULT 50;
IF COL_LENGTH('MilitaryUnits', 'CommanderCorruption') IS NULL
    ALTER TABLE MilitaryUnits ADD CommanderCorruption REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryUnits', 'CommanderExperience') IS NULL
    ALTER TABLE MilitaryUnits ADD CommanderExperience REAL NOT NULL DEFAULT 20;
IF COL_LENGTH('MilitaryUnits', 'Status') IS NULL
    ALTER TABLE MilitaryUnits ADD [Status] NVARCHAR(20) NOT NULL DEFAULT 'Forming';
IF COL_LENGTH('MilitaryUnits', 'IsMobilized') IS NULL
    ALTER TABLE MilitaryUnits ADD IsMobilized BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryUnits', 'FormationProgress') IS NULL
    ALTER TABLE MilitaryUnits ADD FormationProgress REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryUnits', 'FormationBaseDays') IS NULL
    ALTER TABLE MilitaryUnits ADD FormationBaseDays INT NOT NULL DEFAULT 7;
IF COL_LENGTH('MilitaryUnits', 'CreatedAt') IS NULL
    ALTER TABLE MilitaryUnits ADD CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('MilitaryUnits', 'UpdatedAt') IS NULL
    ALTER TABLE MilitaryUnits ADD UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryUnits')
      AND name = 'IX_MilitaryUnits_CountryId'
)
    CREATE INDEX IX_MilitaryUnits_CountryId ON MilitaryUnits (CountryId);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryUnits')
      AND name = 'IX_MilitaryUnits_Status'
)
    CREATE INDEX IX_MilitaryUnits_Status ON MilitaryUnits ([Status]);

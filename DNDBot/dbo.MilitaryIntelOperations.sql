IF OBJECT_ID('MilitaryIntelOperations', 'U') IS NULL
BEGIN
    CREATE TABLE MilitaryIntelOperations (
        IntelLogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SourceCountryId INT NOT NULL,
        TargetCountryId INT NOT NULL,
        OperationType NVARCHAR(40) NOT NULL DEFAULT 'ScoutForces',
        BudgetGold BIGINT NOT NULL DEFAULT 0,
        Success BIT NOT NULL DEFAULT 0,
        Detected BIT NOT NULL DEFAULT 0,
        VisibleToTarget BIT NOT NULL DEFAULT 0,
        SuccessChance REAL NOT NULL DEFAULT 0,
        DetectionChance REAL NOT NULL DEFAULT 0,
        Roll REAL NOT NULL DEFAULT 0,
        SourceTreasuryDelta BIGINT NOT NULL DEFAULT 0,
        TargetTreasuryDelta BIGINT NOT NULL DEFAULT 0,
        SourceIntelDelta REAL NOT NULL DEFAULT 0,
        SourceCounterIntelDelta REAL NOT NULL DEFAULT 0,
        TargetIntelDelta REAL NOT NULL DEFAULT 0,
        TargetCounterIntelDelta REAL NOT NULL DEFAULT 0,
        TargetArmyMoraleDelta REAL NOT NULL DEFAULT 0,
        TargetLogisticsDelta REAL NOT NULL DEFAULT 0,
        AffectedUnits INT NOT NULL DEFAULT 0,
        ReconPowerEstimate REAL NOT NULL DEFAULT 0,
        ReconTroopsEstimate INT NOT NULL DEFAULT 0,
        Report NVARCHAR(1200) NOT NULL DEFAULT N'',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('MilitaryIntelOperations', 'SourceCountryId') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD SourceCountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetCountryId') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetCountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'OperationType') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD OperationType NVARCHAR(40) NOT NULL DEFAULT 'ScoutForces';
IF COL_LENGTH('MilitaryIntelOperations', 'BudgetGold') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD BudgetGold BIGINT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'Success') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD Success BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'Detected') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD Detected BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'VisibleToTarget') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD VisibleToTarget BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'SuccessChance') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD SuccessChance REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'DetectionChance') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD DetectionChance REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'Roll') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD Roll REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'SourceTreasuryDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD SourceTreasuryDelta BIGINT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetTreasuryDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetTreasuryDelta BIGINT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'SourceIntelDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD SourceIntelDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'SourceCounterIntelDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD SourceCounterIntelDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetIntelDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetIntelDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetCounterIntelDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetCounterIntelDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetArmyMoraleDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetArmyMoraleDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'TargetLogisticsDelta') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD TargetLogisticsDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'AffectedUnits') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD AffectedUnits INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'ReconPowerEstimate') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD ReconPowerEstimate REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'ReconTroopsEstimate') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD ReconTroopsEstimate INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryIntelOperations', 'Report') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD Report NVARCHAR(1200) NOT NULL DEFAULT N'';
IF COL_LENGTH('MilitaryIntelOperations', 'CreatedAt') IS NULL
    ALTER TABLE MilitaryIntelOperations ADD CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryIntelOperations')
      AND name = 'IX_MilitaryIntelOperations_Source'
)
    CREATE INDEX IX_MilitaryIntelOperations_Source ON MilitaryIntelOperations (SourceCountryId, CreatedAt DESC);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryIntelOperations')
      AND name = 'IX_MilitaryIntelOperations_TargetVisible'
)
    CREATE INDEX IX_MilitaryIntelOperations_TargetVisible ON MilitaryIntelOperations (TargetCountryId, VisibleToTarget, CreatedAt DESC);

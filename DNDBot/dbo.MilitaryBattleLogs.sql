IF OBJECT_ID('MilitaryBattleLogs', 'U') IS NULL
BEGIN
    CREATE TABLE MilitaryBattleLogs (
        BattleLogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AttackerCountryId INT NOT NULL,
        DefenderCountryId INT NOT NULL,
        Terrain NVARCHAR(30) NOT NULL DEFAULT 'Plains',
        AttackerTactic NVARCHAR(30) NOT NULL DEFAULT 'Balanced',
        DefenderTactic NVARCHAR(30) NOT NULL DEFAULT 'Balanced',
        ResultType NVARCHAR(20) NOT NULL DEFAULT 'Battle',
        WinnerCountryId INT NULL,
        AttackerPower REAL NOT NULL DEFAULT 0,
        DefenderPower REAL NOT NULL DEFAULT 0,
        AttackerTroopsStart INT NOT NULL DEFAULT 0,
        DefenderTroopsStart INT NOT NULL DEFAULT 0,
        AttackerLosses INT NOT NULL DEFAULT 0,
        DefenderLosses INT NOT NULL DEFAULT 0,
        AttackerVassalLosses INT NOT NULL DEFAULT 0,
        DefenderVassalLosses INT NOT NULL DEFAULT 0,
        AttackerMoraleDelta REAL NOT NULL DEFAULT 0,
        DefenderMoraleDelta REAL NOT NULL DEFAULT 0,
        AttackerExperienceGain REAL NOT NULL DEFAULT 0,
        DefenderExperienceGain REAL NOT NULL DEFAULT 0,
        Summary NVARCHAR(600) NOT NULL DEFAULT N'',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('MilitaryBattleLogs', 'AttackerCountryId') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerCountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderCountryId') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderCountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'Terrain') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD Terrain NVARCHAR(30) NOT NULL DEFAULT 'Plains';
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerTactic') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerTactic NVARCHAR(30) NOT NULL DEFAULT 'Balanced';
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderTactic') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderTactic NVARCHAR(30) NOT NULL DEFAULT 'Balanced';
IF COL_LENGTH('MilitaryBattleLogs', 'ResultType') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD ResultType NVARCHAR(20) NOT NULL DEFAULT 'Battle';
IF COL_LENGTH('MilitaryBattleLogs', 'WinnerCountryId') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD WinnerCountryId INT NULL;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerPower') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerPower REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderPower') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderPower REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerTroopsStart') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerTroopsStart INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderTroopsStart') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderTroopsStart INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerLosses') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerLosses INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderLosses') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderLosses INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerVassalLosses') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerVassalLosses INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderVassalLosses') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderVassalLosses INT NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerMoraleDelta') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerMoraleDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderMoraleDelta') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderMoraleDelta REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'AttackerExperienceGain') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD AttackerExperienceGain REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'DefenderExperienceGain') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD DefenderExperienceGain REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('MilitaryBattleLogs', 'Summary') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD Summary NVARCHAR(600) NOT NULL DEFAULT N'';
IF COL_LENGTH('MilitaryBattleLogs', 'CreatedAt') IS NULL
    ALTER TABLE MilitaryBattleLogs ADD CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryBattleLogs')
      AND name = 'IX_MilitaryBattleLogs_Attacker'
)
    CREATE INDEX IX_MilitaryBattleLogs_Attacker ON MilitaryBattleLogs (AttackerCountryId, CreatedAt DESC);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('MilitaryBattleLogs')
      AND name = 'IX_MilitaryBattleLogs_Defender'
)
    CREATE INDEX IX_MilitaryBattleLogs_Defender ON MilitaryBattleLogs (DefenderCountryId, CreatedAt DESC);

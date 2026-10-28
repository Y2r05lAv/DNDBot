IF OBJECT_ID('VassalMilitaryObligations', 'U') IS NULL
BEGIN
    CREATE TABLE VassalMilitaryObligations (
        ObligationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        SubjectName NVARCHAR(100) NOT NULL,
        RegimentsProvided INT NOT NULL DEFAULT 0,
        FundingPerTurn DECIMAL(18,2) NOT NULL DEFAULT 0,
        Loyalty REAL NOT NULL DEFAULT 60,
        RebellionRisk REAL NOT NULL DEFAULT 5,
        IsActive BIT NOT NULL DEFAULT 1,
        LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('VassalMilitaryObligations', 'CountryId') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD CountryId INT NOT NULL DEFAULT 0;
IF COL_LENGTH('VassalMilitaryObligations', 'SubjectName') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD SubjectName NVARCHAR(100) NOT NULL DEFAULT N'Unknown';
IF COL_LENGTH('VassalMilitaryObligations', 'RegimentsProvided') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD RegimentsProvided INT NOT NULL DEFAULT 0;
IF COL_LENGTH('VassalMilitaryObligations', 'FundingPerTurn') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD FundingPerTurn DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('VassalMilitaryObligations', 'Loyalty') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD Loyalty REAL NOT NULL DEFAULT 60;
IF COL_LENGTH('VassalMilitaryObligations', 'RebellionRisk') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD RebellionRisk REAL NOT NULL DEFAULT 5;
IF COL_LENGTH('VassalMilitaryObligations', 'IsActive') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD IsActive BIT NOT NULL DEFAULT 1;
IF COL_LENGTH('VassalMilitaryObligations', 'LastUpdated') IS NULL
    ALTER TABLE VassalMilitaryObligations ADD LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('VassalMilitaryObligations')
      AND name = 'IX_VassalMilitaryObligations_CountryId'
)
    CREATE INDEX IX_VassalMilitaryObligations_CountryId ON VassalMilitaryObligations (CountryId);

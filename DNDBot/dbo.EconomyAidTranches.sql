IF OBJECT_ID('EconomyAidTranches', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyAidTranches (
        TrancheId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FromCountryId INT NOT NULL,
        ToCountryId INT NOT NULL,
        DailyAmount DECIMAL(28,2) NOT NULL,
        RemainingDays INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_EconomyAidTranches_IsActive DEFAULT 1,
        CreatedByUserId BIGINT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EconomyAidTranches_CreatedAt DEFAULT GETDATE(),
        LastAppliedAt DATETIME2 NULL
    );
END;

IF COL_LENGTH('EconomyAidTranches', 'IsActive') IS NULL
BEGIN
    ALTER TABLE EconomyAidTranches ADD IsActive BIT NOT NULL CONSTRAINT DF_EconomyAidTranches_IsActive_Legacy DEFAULT 1;
END;

IF COL_LENGTH('EconomyAidTranches', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE EconomyAidTranches ADD CreatedByUserId BIGINT NOT NULL CONSTRAINT DF_EconomyAidTranches_CreatedByUserId_Legacy DEFAULT 0;
END;

IF COL_LENGTH('EconomyAidTranches', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE EconomyAidTranches ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EconomyAidTranches_CreatedAt_Legacy DEFAULT GETDATE();
END;

IF COL_LENGTH('EconomyAidTranches', 'LastAppliedAt') IS NULL
BEGIN
    ALTER TABLE EconomyAidTranches ADD LastAppliedAt DATETIME2 NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyAidTranches')
      AND name = 'IX_EconomyAidTranches_FromCountry_Active'
)
BEGIN
    CREATE INDEX IX_EconomyAidTranches_FromCountry_Active
        ON EconomyAidTranches (FromCountryId, IsActive, RemainingDays);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('EconomyAidTranches')
      AND name = 'IX_EconomyAidTranches_ToCountry_Active'
)
BEGIN
    CREATE INDEX IX_EconomyAidTranches_ToCountry_Active
        ON EconomyAidTranches (ToCountryId, IsActive, RemainingDays);
END;

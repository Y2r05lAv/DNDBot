IF OBJECT_ID('EconomyCountryEffectsLog', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCountryEffectsLog (
        LogId INT IDENTITY(1,1) NOT NULL,
        CountryId INT NOT NULL,
        UserId BIGINT NOT NULL,
        EffectType NVARCHAR(50) NOT NULL,
        OldValue BIT NOT NULL,
        NewValue BIT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        TurnNumber INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EconomyCountryEffectsLog_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT PK_EconomyCountryEffectsLog PRIMARY KEY (LogId)
    );
END;

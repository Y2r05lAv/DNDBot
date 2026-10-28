IF OBJECT_ID('DiplomacyActionsLog', 'U') IS NULL
BEGIN
    CREATE TABLE DiplomacyActionsLog (
        LogId INT IDENTITY(1,1) NOT NULL,
        CountryId INT NOT NULL,
        TargetCountryId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        OldState INT NOT NULL,
        NewState INT NOT NULL,
        UserId BIGINT NOT NULL,
        TurnNumber INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_DiplomacyActionsLog_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT PK_DiplomacyActionsLog PRIMARY KEY (LogId)
    );
END;

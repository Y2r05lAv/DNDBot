IF OBJECT_ID('EconomyCrisisActionsLog', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyCrisisActionsLog (
        CountryId INT NOT NULL,
        ActionType NVARCHAR(30) NOT NULL,
        LastAppliedTurn INT NOT NULL DEFAULT 0,
        LastAppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_EconomyCrisisActionsLog PRIMARY KEY (CountryId, ActionType)
    );
END;

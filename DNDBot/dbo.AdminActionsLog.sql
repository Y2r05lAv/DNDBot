IF OBJECT_ID('AdminActionsLog', 'U') IS NULL
BEGIN
    CREATE TABLE AdminActionsLog (
        LogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        UserId BIGINT NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        OldValue NVARCHAR(200) NULL,
        NewValue NVARCHAR(200) NULL,
        Details NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;

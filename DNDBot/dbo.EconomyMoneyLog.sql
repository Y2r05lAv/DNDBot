IF OBJECT_ID('EconomyMoneyLog', 'U') IS NULL
BEGIN
    CREATE TABLE EconomyMoneyLog (
        LogId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        UserId BIGINT NOT NULL,
        Amount DECIMAL(28,2) NOT NULL DEFAULT 0,
        Kind NVARCHAR(10) NOT NULL,
        Reason NVARCHAR(200) NOT NULL,
        TurnNumber INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END;

IF COL_LENGTH('EconomyMoneyLog', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE EconomyMoneyLog ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
END;


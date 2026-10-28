IF OBJECT_ID(N'[dbo].[UserData]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UserData', 'TempHP') IS NULL
    BEGIN
        ALTER TABLE [dbo].[UserData]
            ADD [TempHP] INT NOT NULL
            CONSTRAINT [DF_UserData_TempHP] DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.UserData', 'TempMaxHPReduction') IS NULL
    BEGIN
        ALTER TABLE [dbo].[UserData]
            ADD [TempMaxHPReduction] INT NOT NULL
            CONSTRAINT [DF_UserData_TempMaxHPReduction] DEFAULT (0);
    END;
END;

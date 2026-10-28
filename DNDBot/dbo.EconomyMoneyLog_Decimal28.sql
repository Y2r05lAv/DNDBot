IF COL_LENGTH('EconomyMoneyLog', 'Amount') IS NOT NULL
BEGIN
    DECLARE @needs_amount bit = 0;
    SELECT @needs_amount = CASE
        WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
        ELSE 0
    END
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('EconomyMoneyLog')
      AND c.name = 'Amount';

    IF @needs_amount = 1
    BEGIN
        DECLARE @df_amount sysname;
        SELECT @df_amount = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('EconomyMoneyLog')
          AND c.name = 'Amount';

        IF @df_amount IS NOT NULL
            EXEC('ALTER TABLE EconomyMoneyLog DROP CONSTRAINT [' + @df_amount + ']');

        ALTER TABLE EconomyMoneyLog ALTER COLUMN Amount DECIMAL(28,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('EconomyMoneyLog')
          AND c.name = 'Amount'
    )
        ALTER TABLE EconomyMoneyLog ADD CONSTRAINT DF_EconomyMoneyLog_Amount DEFAULT 0 FOR Amount;
END

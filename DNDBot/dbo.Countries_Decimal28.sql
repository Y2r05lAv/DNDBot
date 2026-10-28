IF COL_LENGTH('Countries', 'IncomePerTurn') IS NOT NULL
BEGIN
    DECLARE @needs_income bit = 0;
    SELECT @needs_income = CASE
        WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
        ELSE 0
    END
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'IncomePerTurn';

    IF @needs_income = 1
    BEGIN
        DECLARE @df_income sysname;
        SELECT @df_income = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'IncomePerTurn';

        IF @df_income IS NOT NULL
            EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_income + ']');

        ALTER TABLE Countries ALTER COLUMN IncomePerTurn DECIMAL(28,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'IncomePerTurn'
    )
        ALTER TABLE Countries ADD CONSTRAINT DF_Countries_IncomePerTurn DEFAULT 0 FOR IncomePerTurn;
END

IF COL_LENGTH('Countries', 'ExpensesPerTurn') IS NOT NULL
BEGIN
    DECLARE @needs_exp bit = 0;
    SELECT @needs_exp = CASE
        WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
        ELSE 0
    END
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'ExpensesPerTurn';

    IF @needs_exp = 1
    BEGIN
        DECLARE @df_exp sysname;
        SELECT @df_exp = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'ExpensesPerTurn';

        IF @df_exp IS NOT NULL
            EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_exp + ']');

        ALTER TABLE Countries ALTER COLUMN ExpensesPerTurn DECIMAL(28,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'ExpensesPerTurn'
    )
        ALTER TABLE Countries ADD CONSTRAINT DF_Countries_ExpensesPerTurn DEFAULT 0 FOR ExpensesPerTurn;
END

IF COL_LENGTH('Countries', 'CorruptionLosses') IS NOT NULL
BEGIN
    DECLARE @needs_corr bit = 0;
    SELECT @needs_corr = CASE
        WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
        ELSE 0
    END
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'CorruptionLosses';

    IF @needs_corr = 1
    BEGIN
        DECLARE @df_corr sysname;
        SELECT @df_corr = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'CorruptionLosses';

        IF @df_corr IS NOT NULL
            EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_corr + ']');

        ALTER TABLE Countries ALTER COLUMN CorruptionLosses DECIMAL(28,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'CorruptionLosses'
    )
        ALTER TABLE Countries ADD CONSTRAINT DF_Countries_CorruptionLosses DEFAULT 0 FOR CorruptionLosses;
END

IF COL_LENGTH('Countries', 'MilitaryUpkeep') IS NOT NULL
BEGIN
    DECLARE @needs_mil bit = 0;
    SELECT @needs_mil = CASE
        WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
        ELSE 0
    END
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'MilitaryUpkeep';

    IF @needs_mil = 1
    BEGIN
        DECLARE @df_mil sysname;
        SELECT @df_mil = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'MilitaryUpkeep';

        IF @df_mil IS NOT NULL
            EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_mil + ']');

        ALTER TABLE Countries ALTER COLUMN MilitaryUpkeep DECIMAL(28,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID('Countries')
          AND c.name = 'MilitaryUpkeep'
    )
        ALTER TABLE Countries ADD CONSTRAINT DF_Countries_MilitaryUpkeep DEFAULT 0 FOR MilitaryUpkeep;
END

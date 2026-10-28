IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'AvailableWorkers'
      AND t.name = 'int'
)
BEGIN
    DECLARE @df_avail sysname;
    SELECT @df_avail = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('Countries')
      AND c.name = 'AvailableWorkers';

    IF @df_avail IS NOT NULL
        EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_avail + ']');

    ALTER TABLE Countries ALTER COLUMN AvailableWorkers BIGINT NOT NULL;
END

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'Refugees'
      AND t.name = 'int'
)
BEGIN
    DECLARE @df_ref sysname;
    SELECT @df_ref = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('Countries')
      AND c.name = 'Refugees';

    IF @df_ref IS NOT NULL
        EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_ref + ']');

    ALTER TABLE Countries ALTER COLUMN Refugees BIGINT NOT NULL;
END

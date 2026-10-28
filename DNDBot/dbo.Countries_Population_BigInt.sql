IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Countries')
      AND c.name = 'Population'
      AND t.name = 'int'
)
BEGIN
    DECLARE @df_pop sysname;
    SELECT @df_pop = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('Countries')
      AND c.name = 'Population';

    IF @df_pop IS NOT NULL
        EXEC('ALTER TABLE Countries DROP CONSTRAINT [' + @df_pop + ']');

    ALTER TABLE Countries ALTER COLUMN Population BIGINT NOT NULL;
END

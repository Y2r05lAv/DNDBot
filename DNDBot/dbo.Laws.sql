IF OBJECT_ID('Laws', 'U') IS NULL
BEGIN
    CREATE TABLE Laws (
        LawId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Laws_Status DEFAULT ('Review1'),
        EffectType NVARCHAR(500) NOT NULL,
        EffectValue DECIMAL(10,2) NOT NULL CONSTRAINT DF_Laws_EffectValue DEFAULT (0),
        CreatedByCharacterId INT NOT NULL CONSTRAINT DF_Laws_CreatedBy DEFAULT (0),
        ExpiresAt DATETIME2 NULL,
        ParentLawId INT NULL
    );
END;

IF COL_LENGTH('Laws', 'ParentLawId') IS NULL
BEGIN
    ALTER TABLE Laws ADD ParentLawId INT NULL;
END;

IF COL_LENGTH('Laws', 'EffectType') IS NOT NULL AND COL_LENGTH('Laws', 'EffectType') < 1000
BEGIN
    ALTER TABLE Laws ALTER COLUMN EffectType NVARCHAR(500) NOT NULL;
END;

IF COL_LENGTH('Laws', 'Title') IS NOT NULL AND COL_LENGTH('Laws', 'Title') < 1000
BEGIN
    ALTER TABLE Laws ALTER COLUMN Title NVARCHAR(500) NOT NULL;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('Laws')
      AND name = 'Description'
      AND max_length <> -1
)
BEGIN
    ALTER TABLE Laws ALTER COLUMN Description NVARCHAR(MAX) NULL;
END;

DECLARE @StatusDefaultConstraintName NVARCHAR(128);
DECLARE @StatusDefaultDefinition NVARCHAR(4000);

SELECT TOP (1)
    @StatusDefaultConstraintName = dc.name,
    @StatusDefaultDefinition = dc.definition
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('Laws')
  AND c.name = 'Status';

IF @StatusDefaultConstraintName IS NULL OR @StatusDefaultDefinition NOT LIKE '%Review1%'
BEGIN
    IF @StatusDefaultConstraintName IS NOT NULL
    BEGIN
        DECLARE @DropStatusDefaultSql NVARCHAR(MAX);
        SET @DropStatusDefaultSql = N'ALTER TABLE Laws DROP CONSTRAINT [' + REPLACE(@StatusDefaultConstraintName, ']', ']]') + N'];';
        EXEC(@DropStatusDefaultSql);
    END;

    ALTER TABLE Laws ADD CONSTRAINT DF_Laws_Status DEFAULT ('Review1') FOR Status;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('Laws')
      AND name = 'IX_Laws_CountryId_Status'
)
BEGIN
    CREATE INDEX IX_Laws_CountryId_Status ON Laws (CountryId, Status);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('Laws')
      AND name = 'IX_Laws_CountryId_ParentLawId'
)
BEGIN
    IF COL_LENGTH('Laws', 'ParentLawId') IS NOT NULL
    BEGIN
        EXEC(N'CREATE INDEX IX_Laws_CountryId_ParentLawId ON Laws (CountryId, ParentLawId);');
    END;
END;

IF OBJECT_ID('EconomyTurnHistory', 'U') IS NOT NULL
BEGIN
    DECLARE @cols TABLE (Name sysname);
    INSERT INTO @cols (Name) VALUES
        ('Income'),
        ('Expenses'),
        ('NetResult'),
        ('CorruptionLoss'),
        ('ProductionValue'),
        ('TradeIncome'),
        ('TaxIncome'),
        ('FoodDemand'),
        ('ClothingDemand'),
        ('HousingDemand'),
        ('ToolsDemand'),
        ('LuxuryDemand'),
        ('BlackMarketPurchased'),
        ('BlackMarketValue');

    DECLARE @col sysname;
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @cols;
    OPEN cur;
    FETCH NEXT FROM cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @needs bit = 0;
        SELECT @needs = CASE
            WHEN t.name IN ('decimal', 'numeric') AND (c.precision < 28 OR c.scale <> 2) THEN 1
            ELSE 0
        END
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('EconomyTurnHistory')
          AND c.name = @col;

        IF @needs = 1
        BEGIN
            DECLARE @df sysname;
            SELECT @df = dc.name
            FROM sys.default_constraints dc
            JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
            WHERE dc.parent_object_id = OBJECT_ID('EconomyTurnHistory')
              AND c.name = @col;

            IF @df IS NOT NULL
                EXEC('ALTER TABLE EconomyTurnHistory DROP CONSTRAINT [' + @df + ']');

            EXEC('ALTER TABLE EconomyTurnHistory ALTER COLUMN [' + @col + '] DECIMAL(28,2) NOT NULL;');
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
            WHERE dc.parent_object_id = OBJECT_ID('EconomyTurnHistory')
              AND c.name = @col
        )
            EXEC('ALTER TABLE EconomyTurnHistory ADD CONSTRAINT DF_EconomyTurnHistory_' + @col + ' DEFAULT 0 FOR [' + @col + '];');

        FETCH NEXT FROM cur INTO @col;
    END
    CLOSE cur;
    DEALLOCATE cur;
END

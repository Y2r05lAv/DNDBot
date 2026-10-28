IF COL_LENGTH('Countries', 'IntelLevel') IS NULL
BEGIN
    ALTER TABLE Countries ADD IntelLevel REAL NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('Countries', 'CounterIntelLevel') IS NULL
BEGIN
    ALTER TABLE Countries ADD CounterIntelLevel REAL NOT NULL DEFAULT 0;
END;

IF COL_LENGTH('Countries', 'WorkRate') IS NULL
    ALTER TABLE Countries ADD WorkRate REAL NOT NULL DEFAULT 60;
IF COL_LENGTH('Countries', 'AvailableWorkers') IS NULL
    ALTER TABLE Countries ADD AvailableWorkers BIGINT NOT NULL DEFAULT 0;
IF COL_LENGTH('Countries', 'UnemploymentRate') IS NULL
    ALTER TABLE Countries ADD UnemploymentRate REAL NOT NULL DEFAULT 0;
IF COL_LENGTH('Countries', 'WarState') IS NULL
    ALTER TABLE Countries ADD WarState BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Countries', 'SanctionsLevel') IS NULL
    ALTER TABLE Countries ADD SanctionsLevel INT NOT NULL DEFAULT 0;

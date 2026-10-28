IF OBJECT_ID('ResearchEffects', 'U') IS NULL
BEGIN
    CREATE TABLE ResearchEffects (
        ResearchId INT NOT NULL,
        EffectType INT NOT NULL,
        EffectValue NVARCHAR(100) NULL,
        EffectAmount REAL NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        CONSTRAINT PK_ResearchEffects PRIMARY KEY (ResearchId, EffectType, SortOrder)
    );
END;


DECLARE @id INT;

SELECT @id = ResearchId FROM ResearchDefinitions WHERE Name = N'Писемність';
IF @id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchEffects WHERE ResearchId = @id AND EffectType = 2)
    INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
    VALUES (@id, 2, N'AdminEfficiency', 2, 0);

SELECT @id = ResearchId FROM ResearchDefinitions WHERE Name = N'Бюрократія';
IF @id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchEffects WHERE ResearchId = @id AND EffectType = 4)
    INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
    VALUES (@id, 4, NULL, 1, 0);

SELECT @id = ResearchId FROM ResearchDefinitions WHERE Name = N'Парламент';
IF @id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchEffects WHERE ResearchId = @id AND EffectType = 4)
    INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
    VALUES (@id, 4, NULL, 1, 0);
IF @id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchEffects WHERE ResearchId = @id AND EffectType = 5)
    INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
    VALUES (@id, 5, NULL, 10, 1);

SELECT @id = ResearchId FROM ResearchDefinitions WHERE Name = N'Архіви';
IF @id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchEffects WHERE ResearchId = @id AND EffectType = 7)
    INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
    VALUES (@id, 7, NULL, 10, 0);

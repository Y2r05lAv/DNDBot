IF OBJECT_ID('ResearchRequirements', 'U') IS NULL
BEGIN
    CREATE TABLE ResearchRequirements (
        ResearchId INT NOT NULL,
        RequiredResearchId INT NOT NULL,
        CONSTRAINT PK_ResearchRequirements PRIMARY KEY (ResearchId, RequiredResearchId)
    );
END;


DECLARE @r1 INT, @r2 INT, @r3 INT;
SELECT @r1 = ResearchId FROM ResearchDefinitions WHERE Name = N'Писемність';
SELECT @r2 = ResearchId FROM ResearchDefinitions WHERE Name = N'Бюрократія';
SELECT @r3 = ResearchId FROM ResearchDefinitions WHERE Name = N'Парламент';

IF @r1 IS NOT NULL AND @r2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchRequirements WHERE ResearchId = @r2 AND RequiredResearchId = @r1)
    INSERT INTO ResearchRequirements (ResearchId, RequiredResearchId) VALUES (@r2, @r1);
IF @r2 IS NOT NULL AND @r3 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ResearchRequirements WHERE ResearchId = @r3 AND RequiredResearchId = @r2)
    INSERT INTO ResearchRequirements (ResearchId, RequiredResearchId) VALUES (@r3, @r2);

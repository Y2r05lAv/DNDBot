CREATE TABLE ResearchProgress (
    CountryId INT NOT NULL,
    ResearchId INT NOT NULL,
    Progress REAL NOT NULL DEFAULT 0,
    StartedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsCompleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_ResearchProgress PRIMARY KEY (CountryId, ResearchId)
);

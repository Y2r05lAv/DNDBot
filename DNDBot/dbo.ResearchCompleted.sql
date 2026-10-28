CREATE TABLE ResearchCompleted (
    CountryId INT NOT NULL,
    ResearchId INT NOT NULL,
    CompletedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_ResearchCompleted PRIMARY KEY (CountryId, ResearchId)
);

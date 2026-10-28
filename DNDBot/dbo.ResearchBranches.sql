IF OBJECT_ID('ResearchBranches', 'U') IS NULL
BEGIN
    CREATE TABLE ResearchBranches (
        BranchName NVARCHAR(50) NOT NULL PRIMARY KEY,
        SortOrder INT NOT NULL DEFAULT 0
    );
END;

IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Адміністрація')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Адміністрація', 10);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Воєнна')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Воєнна', 20);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Наука')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Наука', 30);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Економіка')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Економіка', 40);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Право')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Право', 50);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Дипломатія')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Дипломатія', 60);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Магія')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Магія', 70);
IF NOT EXISTS (SELECT 1 FROM ResearchBranches WHERE BranchName = N'Загальне')
    INSERT INTO ResearchBranches (BranchName, SortOrder) VALUES (N'Загальне', 80);

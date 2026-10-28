IF OBJECT_ID('ResearchDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE ResearchDefinitions (
        ResearchId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Branch NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        ScienceCost INT NOT NULL,
        MoneyCost BIGINT NOT NULL,
        DurationDays INT NOT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
END;


IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Писемність')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Писемність', N'Адміністрація', N'Базові навички письма та обліку.', 50, 2000, 3, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Бюрократія')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Бюрократія', N'Адміністрація', N'Формалізація управління та правил.', 120, 8000, 5, 20, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Парламент')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Парламент', N'Адміністрація', N'Утворення парламентських інститутів.', 180, 12000, 6, 30, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Реформа армії')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Реформа армії', N'Воєнна', N'Базові зміни організації військ.', 90, 6000, 4, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Військова доктрина')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Військова доктрина', N'Воєнна', N'Формування стратегій і тактики.', 160, 12000, 6, 20, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Архіви')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Архіви', N'Наука', N'Систематизація знань та їх збереження.', 90, 5000, 4, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Лабораторії')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Лабораторії', N'Наука', N'Організація наукових лабораторій.', 150, 11000, 6, 20, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Облік ресурсів')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Облік ресурсів', N'Економіка', N'Стандартизація економічного обліку.', 80, 4000, 3, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Податкова реформа')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Податкова реформа', N'Економіка', N'Оптимізація податкових механізмів.', 140, 9000, 5, 20, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Кодекс законів')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Кодекс законів', N'Право', N'Кодифікація основних законів.', 120, 7000, 5, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Договірна база')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Договірна база', N'Дипломатія', N'Стандартизація дипломатичних угод.', 110, 6500, 4, 10, 1);
IF NOT EXISTS (SELECT 1 FROM ResearchDefinitions WHERE Name = N'Магічні канали')
    INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
    VALUES (N'Магічні канали', N'Магія', N'Налагодження магічної інфраструктури.', 130, 9000, 5, 10, 1);

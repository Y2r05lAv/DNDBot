IF OBJECT_ID(N'[dbo].[MonsterTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MonsterTemplates]
    (
        [TemplateId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(128) NOT NULL,
        [HP] INT NOT NULL,
        [Armour] INT NOT NULL,
        [XP] INT NOT NULL CONSTRAINT [DF_MonsterTemplates_XP] DEFAULT (0),
        [AttackName] NVARCHAR(128) NULL,
        [AttackDamageDice] NVARCHAR(128) NULL,
        [CreatedBy] BIGINT NOT NULL,
        [CreatedAt] DATETIME2(0) NOT NULL CONSTRAINT [DF_MonsterTemplates_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_MonsterTemplates] PRIMARY KEY CLUSTERED ([TemplateId] ASC)
    );
END;

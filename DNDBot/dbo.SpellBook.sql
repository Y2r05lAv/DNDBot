CREATE TABLE [dbo].[SpellBook] (
    [Id]              BIGINT         NOT NULL,
    [Name]            NVARCHAR (50)  NOT NULL,
    [Description]     NVARCHAR (MAX) NOT NULL,
    [TypeHarm]        NVARCHAR (50)  NOT NULL,
    [NumberOfTargets] INT            NOT NULL,
    [Target]          NVARCHAR (50)  NOT NULL,
    [DamageDice]      NVARCHAR (50)  NOT NULL,
    [Modifier]        NCHAR (10)     NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


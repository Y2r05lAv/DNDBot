CREATE TABLE [dbo].[UserData] (
    [Id]           BIGINT           NOT NULL,
    [Strenght]     TINYINT           NOT NULL,
    [Agility]      TINYINT           NOT NULL,
    [Stamina]      TINYINT           NOT NULL,
    [Intelligence] TINYINT           NOT NULL,
    [Wisdom]       TINYINT           NOT NULL,
    [Charisma]     TINYINT           NOT NULL,
    [Health]       INT           NOT NULL,
    [Armor]        TINYINT           NOT NULL,
    [Name]         NVARCHAR (50) NOT NULL,
    [CopperCoin] INT NOT NULL, 
    [SilverCoin] INT NOT NULL, 
    [GoldenCoin] INT NOT NULL, 
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


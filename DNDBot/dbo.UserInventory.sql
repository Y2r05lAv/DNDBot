CREATE TABLE [dbo].[UserInventory] (
    [Id]         BIGINT            NOT NULL,
	[ItemId]     INT IDENTITY(1,1) NOT NULL,
    [ItemName]   NVARCHAR (50) NOT NULL,
    [ItemAmount] INT           NOT NULL,
    [ItemWeight] FLOAT (53)    NOT NULL,
    PRIMARY KEY CLUSTERED ([Id],[ItemId] ASC)
);


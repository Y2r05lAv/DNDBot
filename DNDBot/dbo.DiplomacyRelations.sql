IF OBJECT_ID('DiplomacyRelations', 'U') IS NULL
BEGIN
    CREATE TABLE DiplomacyRelations (
        CountryA INT NOT NULL,
        CountryB INT NOT NULL,
        State INT NOT NULL,
        IsAlliance BIT NOT NULL,
        IsAtWar BIT NOT NULL,
        HasSanctions BIT NOT NULL,
        HasEmbargo BIT NOT NULL,
        HasTradeAgreement BIT NOT NULL,
        LastChangedTurn INT NULL,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_DiplomacyRelations_UpdatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT PK_DiplomacyRelations PRIMARY KEY (CountryA, CountryB)
    );
END;

IF OBJECT_ID('Parliament', 'U') IS NULL
BEGIN
    CREATE TABLE Parliament (
        ParliamentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        TotalSeats INT NOT NULL CONSTRAINT DF_Parliament_TotalSeats DEFAULT (0),
        QuorumPercent DECIMAL(4,2) NOT NULL CONSTRAINT DF_Parliament_QuorumPercent DEFAULT (60),
        GovernmentSupport DECIMAL(4,2) NOT NULL CONSTRAINT DF_Parliament_GovSupport DEFAULT (50),
        IsSessionActive BIT NOT NULL CONSTRAINT DF_Parliament_IsSessionActive DEFAULT (0),
        LastSessionStart DATETIME2 NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('Parliament')
      AND name = 'UX_Parliament_CountryId'
)
BEGIN
    CREATE UNIQUE INDEX UX_Parliament_CountryId ON Parliament (CountryId);
END;

IF OBJECT_ID('Parties', 'U') IS NULL
BEGIN
    CREATE TABLE Parties (
        PartyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Ideology NVARCHAR(100) NULL,
        Discipline INT NOT NULL CONSTRAINT DF_Parties_Discipline DEFAULT (50),
        GovernmentAligned BIT NOT NULL CONSTRAINT DF_Parties_GovernmentAligned DEFAULT (0),
        Influence INT NOT NULL CONSTRAINT DF_Parties_Influence DEFAULT (0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('Parties')
      AND name = 'IX_Parties_CountryId'
)
BEGIN
    CREATE INDEX IX_Parties_CountryId ON Parties (CountryId);
END;

IF OBJECT_ID('Deputies', 'U') IS NULL
BEGIN
    CREATE TABLE Deputies (
        DeputyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        PartyId INT NULL,
        Name NVARCHAR(100) NOT NULL,
        Loyalty INT NOT NULL CONSTRAINT DF_Deputies_Loyalty DEFAULT (50),
        Corruption INT NOT NULL CONSTRAINT DF_Deputies_Corruption DEFAULT (0),
        Influence INT NOT NULL CONSTRAINT DF_Deputies_Influence DEFAULT (50),
        AttendanceChance INT NOT NULL CONSTRAINT DF_Deputies_Attendance DEFAULT (75),
        IsAlive BIT NOT NULL CONSTRAINT DF_Deputies_IsAlive DEFAULT (1)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('Deputies')
      AND name = 'IX_Deputies_CountryId'
)
BEGIN
    CREATE INDEX IX_Deputies_CountryId ON Deputies (CountryId);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('Deputies')
      AND name = 'IX_Deputies_CountryId_IsAlive'
)
BEGIN
    CREATE INDEX IX_Deputies_CountryId_IsAlive ON Deputies (CountryId, IsAlive);
END;

IF OBJECT_ID('PartyMembers', 'U') IS NULL
BEGIN
    CREATE TABLE PartyMembers (
        PartyId INT NOT NULL,
        DeputyId INT NOT NULL,
        CONSTRAINT PK_PartyMembers PRIMARY KEY (PartyId, DeputyId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('PartyMembers')
      AND name = 'IX_PartyMembers_DeputyId'
)
BEGIN
    CREATE INDEX IX_PartyMembers_DeputyId ON PartyMembers (DeputyId);
END;

IF OBJECT_ID('ParliamentSessions', 'U') IS NULL
BEGIN
    CREATE TABLE ParliamentSessions (
        SessionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CountryId INT NOT NULL,
        CalledByCharacterId INT NOT NULL CONSTRAINT DF_ParliamentSessions_CalledBy DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ParliamentSessions_Status DEFAULT ('Planned'),
        RequiredQuorum INT NOT NULL CONSTRAINT DF_ParliamentSessions_Quorum DEFAULT (0),
        PresentCount INT NOT NULL CONSTRAINT DF_ParliamentSessions_Present DEFAULT (0),
        StartTime DATETIME2 NOT NULL CONSTRAINT DF_ParliamentSessions_Start DEFAULT (SYSUTCDATETIME()),
        EndTime DATETIME2 NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('ParliamentSessions')
      AND name = 'IX_ParliamentSessions_CountryId_Status'
)
BEGIN
    CREATE INDEX IX_ParliamentSessions_CountryId_Status ON ParliamentSessions (CountryId, Status);
END;

IF OBJECT_ID('SessionAttendance', 'U') IS NULL
BEGIN
    CREATE TABLE SessionAttendance (
        SessionId INT NOT NULL,
        DeputyId INT NOT NULL,
        IsPresent BIT NOT NULL,
        Reason NVARCHAR(50) NULL,
        CONSTRAINT PK_SessionAttendance PRIMARY KEY (SessionId, DeputyId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('SessionAttendance')
      AND name = 'IX_SessionAttendance_SessionId_IsPresent'
)
BEGIN
    CREATE INDEX IX_SessionAttendance_SessionId_IsPresent ON SessionAttendance (SessionId, IsPresent);
END;

IF OBJECT_ID('Votes', 'U') IS NULL
BEGIN
    CREATE TABLE Votes (
        LawId INT NOT NULL,
        DeputyId INT NOT NULL,
        VoteType NVARCHAR(10) NOT NULL,
        CONSTRAINT PK_Votes PRIMARY KEY (LawId, DeputyId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('Votes')
      AND name = 'IX_Votes_DeputyId'
)
BEGIN
    CREATE INDEX IX_Votes_DeputyId ON Votes (DeputyId);
END;

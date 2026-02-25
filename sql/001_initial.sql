-- Bangkok API - Initial schema
-- SQL Server. Primary keys: UNIQUEIDENTIFIER. Use DATETIME2, NVARCHAR.

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dbo')
    EXEC('CREATE SCHEMA dbo');

-- User table
IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Email           NVARCHAR(256)    NOT NULL,
        DisplayName     NVARCHAR(256)    NULL,
        PasswordHash    NVARCHAR(500)    NOT NULL,
        PasswordSalt    NVARCHAR(500)    NOT NULL,
        Role                NVARCHAR(64)     NOT NULL DEFAULT N'User',
        IsActive            BIT              NOT NULL DEFAULT 1,
        CreatedAtUtc        DATETIME2(7)     NOT NULL,
        UpdatedAtUtc        DATETIME2(7)     NULL,
        RecoverString       NVARCHAR(200)    NULL,
        RecoverStringExpiry DATETIME2(7)     NULL,
        CONSTRAINT UQ_User_Email UNIQUE (Email)
    );
    CREATE NONCLUSTERED INDEX IX_User_Email ON dbo.[User] (Email);
END;

-- RefreshToken table
IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        Token           NVARCHAR(500)    NOT NULL,
        ExpiresAtUtc    DATETIME2(7)     NOT NULL,
        CreatedAtUtc    DATETIME2(7)     NOT NULL,
        RevokedReason   NVARCHAR(256)    NULL,
        RevokedAtUtc    DATETIME2(7)     NULL,
        CONSTRAINT FK_RefreshToken_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_RefreshToken_Token ON dbo.RefreshToken (Token);
    CREATE NONCLUSTERED INDEX IX_RefreshToken_UserId ON dbo.RefreshToken (UserId);
END;

GO

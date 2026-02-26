-- Add Profiles table (1:1 with User). Run after 006.
-- Always update 001_initial.sql to reflect full schema; keep this alter for history.

IF OBJECT_ID(N'dbo.Profile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Profile
    (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        FirstName       NVARCHAR(100)    NOT NULL,
        MiddleName      NVARCHAR(100)    NULL,
        LastName        NVARCHAR(100)    NOT NULL,
        DateOfBirth     DATETIME2(7)     NOT NULL,
        PhoneNumber     NVARCHAR(30)    NULL,
        AvatarBase64    NVARCHAR(MAX)    NULL,
        CreatedAtUtc    DATETIME2(7)    NOT NULL,
        UpdatedAtUtc   DATETIME2(7)    NULL,
        CONSTRAINT FK_Profile_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id),
        CONSTRAINT UQ_Profile_UserId UNIQUE (UserId)
    );
    CREATE NONCLUSTERED INDEX IX_Profile_UserId ON dbo.Profile (UserId);
END;

GO

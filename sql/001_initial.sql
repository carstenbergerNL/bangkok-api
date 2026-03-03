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
        IsActive            BIT              NOT NULL DEFAULT 1,
        CreatedAtUtc        DATETIME2(7)     NOT NULL,
        UpdatedAtUtc        DATETIME2(7)     NULL,
        RecoverString       NVARCHAR(200)    NULL,
        RecoverStringExpiry DATETIME2(7)     NULL,
        IsDeleted           BIT              NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2(7)     NULL,
        FailedLoginAttempts INT              NOT NULL DEFAULT 0,
        LockoutEnd          DATETIME2(7)     NULL,
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

-- Role table
IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name        NVARCHAR(100)    NOT NULL,
        Description NVARCHAR(255)    NULL,
        CreatedAt   DATETIME2(7)     NOT NULL,
        CONSTRAINT UQ_Role_Name UNIQUE (Name)
    );
    CREATE NONCLUSTERED INDEX IX_Role_Name ON dbo.Role (Name);
END;

-- UserRole table
IF OBJECT_ID(N'dbo.UserRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId    UNIQUEIDENTIFIER NOT NULL,
        RoleId    UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(Id),
        CONSTRAINT UQ_UserRole_UserId_RoleId UNIQUE (UserId, RoleId)
    );
    CREATE NONCLUSTERED INDEX IX_UserRole_UserId ON dbo.UserRole (UserId);
    CREATE NONCLUSTERED INDEX IX_UserRole_RoleId ON dbo.UserRole (RoleId);
END;

-- Permission table
IF OBJECT_ID(N'dbo.Permission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permission
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name        NVARCHAR(100)    NOT NULL,
        Description NVARCHAR(255)    NULL,
        CONSTRAINT UQ_Permission_Name UNIQUE (Name)
    );
    CREATE NONCLUSTERED INDEX IX_Permission_Name ON dbo.Permission (Name);
END;

-- RolePermission table
IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission
    (
        Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        RoleId       UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(Id),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id),
        CONSTRAINT UQ_RolePermission_RoleId_PermissionId UNIQUE (RoleId, PermissionId)
    );
    CREATE NONCLUSTERED INDEX IX_RolePermission_RoleId ON dbo.RolePermission (RoleId);
    CREATE NONCLUSTERED INDEX IX_RolePermission_PermissionId ON dbo.RolePermission (PermissionId);
END;

-- Profile table (1:1 with User)
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

-- Project table
IF OBJECT_ID(N'dbo.Project', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Project
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name              NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NULL,
        Status            NVARCHAR(50)     NOT NULL,
        CreatedByUserId   UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)     NOT NULL,
        UpdatedAt         DATETIME2(7)     NULL,
        CONSTRAINT FK_Project_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Project_CreatedByUserId ON dbo.Project (CreatedByUserId);
    CREATE NONCLUSTERED INDEX IX_Project_Status ON dbo.Project (Status);
END;

-- Task table
IF OBJECT_ID(N'dbo.Task', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Task
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId         UNIQUEIDENTIFIER NOT NULL,
        Title             NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NULL,
        Status            NVARCHAR(50)     NOT NULL,
        Priority          NVARCHAR(50)     NOT NULL,
        AssignedToUserId  UNIQUEIDENTIFIER NULL,
        DueDate           DATETIME2(7)     NULL,
        CreatedByUserId   UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)     NOT NULL,
        UpdatedAt         DATETIME2(7)     NULL,
        CONSTRAINT FK_Task_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id),
        CONSTRAINT FK_Task_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_Task_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Task_ProjectId ON dbo.Task (ProjectId);
    CREATE NONCLUSTERED INDEX IX_Task_AssignedToUserId ON dbo.Task (AssignedToUserId);
    CREATE NONCLUSTERED INDEX IX_Task_Status ON dbo.Task (Status);
END;

-- TaskComments table
IF OBJECT_ID(N'dbo.TaskComment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskComment
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId    UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Content   NVARCHAR(2000)   NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        UpdatedAt DATETIME2(7)     NULL,
        CONSTRAINT FK_TaskComment_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id),
        CONSTRAINT FK_TaskComment_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskComment_TaskId ON dbo.TaskComment (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskComment_UserId ON dbo.TaskComment (UserId);
END;

-- TaskActivities table
IF OBJECT_ID(N'dbo.TaskActivity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskActivity
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId    UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Action    NVARCHAR(100)    NOT NULL,
        OldValue  NVARCHAR(1000)   NULL,
        NewValue  NVARCHAR(1000)   NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_TaskActivity_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id),
        CONSTRAINT FK_TaskActivity_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskActivity_TaskId ON dbo.TaskActivity (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskActivity_CreatedAt ON dbo.TaskActivity (CreatedAt DESC);
END;

GO

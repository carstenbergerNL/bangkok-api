-- Role Management: Roles, UserRoles, Permissions, RolePermissions
-- Backward compatible: User.Role column retained.

-- Roles
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

-- UserRoles
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

-- Permissions
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

-- RolePermissions
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

GO

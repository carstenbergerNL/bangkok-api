-- Multi-tenancy: Tenants, TenantUsers; Project and Label get TenantId.
-- SQL Server. Run after 020.

-- Tenants table
IF OBJECT_ID(N'dbo.Tenant', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenant
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name      NVARCHAR(200)    NOT NULL,
        Slug      NVARCHAR(100)    NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT UQ_Tenant_Slug UNIQUE (Slug)
    );
    CREATE NONCLUSTERED INDEX IX_Tenant_Slug ON dbo.Tenant (Slug);
END;

-- TenantUser table (user membership in a tenant with role)
IF OBJECT_ID(N'dbo.TenantUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantUser
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId  UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Role      NVARCHAR(50)     NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_TenantUser_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantUser_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id) ON DELETE CASCADE,
        CONSTRAINT UQ_TenantUser_TenantId_UserId UNIQUE (TenantId, UserId)
    );
    CREATE NONCLUSTERED INDEX IX_TenantUser_TenantId ON dbo.TenantUser (TenantId);
    CREATE NONCLUSTERED INDEX IX_TenantUser_UserId ON dbo.TenantUser (UserId);
END;

-- Add TenantId to Project (nullable first for migration)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Project') AND name = 'TenantId')
BEGIN
    ALTER TABLE dbo.Project ADD TenantId UNIQUEIDENTIFIER NULL;
END;

-- Add TenantId to Label (nullable first for migration)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Label') AND name = 'TenantId')
BEGIN
    ALTER TABLE dbo.Label ADD TenantId UNIQUEIDENTIFIER NULL;
END;

-- Migration: create default tenant and assign existing data
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Project') AND name = 'TenantId')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tenant')
BEGIN
    DECLARE @DefaultTenantId UNIQUEIDENTIFIER;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tenant WHERE Slug = N'default')
    BEGIN
        SET @DefaultTenantId = NEWID();
        INSERT INTO dbo.Tenant (Id, Name, Slug, CreatedAt)
        VALUES (@DefaultTenantId, N'Default', N'default', GETUTCDATE());
    END
    ELSE
        SET @DefaultTenantId = (SELECT Id FROM dbo.Tenant WHERE Slug = N'default');

    UPDATE dbo.Project SET TenantId = @DefaultTenantId WHERE TenantId IS NULL;
    UPDATE dbo.Label SET l.TenantId = p.TenantId
    FROM dbo.Label l
    INNER JOIN dbo.Project p ON p.Id = l.ProjectId
    WHERE l.TenantId IS NULL;

    -- Add FK and NOT NULL for Project
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Project_Tenant')
    BEGIN
        ALTER TABLE dbo.Project ADD CONSTRAINT FK_Project_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id);
        CREATE NONCLUSTERED INDEX IX_Project_TenantId ON dbo.Project (TenantId);
    END
    ALTER TABLE dbo.Project ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

    -- Add FK and NOT NULL for Label
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Label_Tenant')
    BEGIN
        ALTER TABLE dbo.Label ADD CONSTRAINT FK_Label_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id);
        CREATE NONCLUSTERED INDEX IX_Label_TenantId ON dbo.Label (TenantId);
    END
    ALTER TABLE dbo.Label ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
END;

GO

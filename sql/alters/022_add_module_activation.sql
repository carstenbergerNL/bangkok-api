-- Module Activation: Tenants can enable/disable modules (Project Management, CRM, Analytics, etc.).
-- SQL Server. Run after 021.

-- Module table (global catalog of available modules)
IF OBJECT_ID(N'dbo.Module', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Module
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name        NVARCHAR(200)    NOT NULL,
        [Key]       NVARCHAR(100)    NOT NULL,
        Description NVARCHAR(500)    NULL,
        CONSTRAINT UQ_Module_Key UNIQUE ([Key])
    );
    CREATE NONCLUSTERED INDEX IX_Module_Key ON dbo.Module ([Key]);
END;

-- TenantModule table (which modules are active per tenant)
IF OBJECT_ID(N'dbo.TenantModule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantModule
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId  UNIQUEIDENTIFIER NOT NULL,
        ModuleId  UNIQUEIDENTIFIER NOT NULL,
        IsActive  BIT              NOT NULL DEFAULT 1,
        CONSTRAINT FK_TenantModule_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModule_Module FOREIGN KEY (ModuleId) REFERENCES dbo.Module(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_TenantModule_TenantId_ModuleId UNIQUE (TenantId, ModuleId)
    );
    CREATE NONCLUSTERED INDEX IX_TenantModule_TenantId ON dbo.TenantModule (TenantId);
    CREATE NONCLUSTERED INDEX IX_TenantModule_ModuleId ON dbo.TenantModule (ModuleId);
END;

-- Seed default modules if not present
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Module')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Module WHERE [Key] = N'ProjectManagement')
        INSERT INTO dbo.Module (Id, Name, [Key], Description)
        VALUES (NEWID(), N'Project Management', N'ProjectManagement', N'Projects, tasks, labels, and collaboration.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Module WHERE [Key] = N'CRM')
        INSERT INTO dbo.Module (Id, Name, [Key], Description)
        VALUES (NEWID(), N'CRM', N'CRM', N'Customer relationship management.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Module WHERE [Key] = N'Analytics')
        INSERT INTO dbo.Module (Id, Name, [Key], Description)
        VALUES (NEWID(), N'Analytics', N'Analytics', N'Reports and analytics.');
END;

-- Activate Project Management for all existing tenants (default-on for existing tenants)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TenantModule')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tenant')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Module')
BEGIN
    INSERT INTO dbo.TenantModule (Id, TenantId, ModuleId, IsActive)
    SELECT NEWID(), t.Id, m.Id, 1
    FROM dbo.Tenant t
    CROSS JOIN dbo.Module m
    WHERE m.[Key] = N'ProjectManagement'
      AND NOT EXISTS (SELECT 1 FROM dbo.TenantModule tm WHERE tm.TenantId = t.Id AND tm.ModuleId = m.Id);
END;

GO

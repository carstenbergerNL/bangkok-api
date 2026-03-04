-- User-level module access: Tenant Admin can grant/revoke per-user access to modules.
-- Rules: User must belong to Tenant; Module must be active for Tenant; unique (TenantId, ModuleId, UserId).
-- Run after 021 (multi-tenant). Update 001_initial.sql to reflect full schema.

IF OBJECT_ID(N'dbo.TenantModuleUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantModuleUser
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId  UNIQUEIDENTIFIER NOT NULL,
        ModuleId  UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_TenantModuleUser_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModuleUser_Module FOREIGN KEY (ModuleId) REFERENCES dbo.[Module](Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModuleUser_User   FOREIGN KEY (UserId)   REFERENCES dbo.[User](Id) ON DELETE CASCADE,
        CONSTRAINT UQ_TenantModuleUser_TenantId_ModuleId_UserId UNIQUE (TenantId, ModuleId, UserId)
    );
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_TenantId ON dbo.TenantModuleUser (TenantId);
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_ModuleId ON dbo.TenantModuleUser (ModuleId);
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_UserId   ON dbo.TenantModuleUser (UserId);
END;

-- Backfill: grant all current tenant users access to all active modules (preserve existing behavior).
-- Remove this block if you want explicit grant-only from the start.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TenantModuleUser')
BEGIN
    INSERT INTO dbo.TenantModuleUser (Id, TenantId, ModuleId, UserId, CreatedAt)
    SELECT NEWID(), tm.TenantId, tm.ModuleId, tu.UserId, GETUTCDATE()
    FROM dbo.TenantModule tm
    INNER JOIN dbo.TenantUser tu ON tu.TenantId = tm.TenantId
    WHERE tm.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM dbo.TenantModuleUser tmu
        WHERE tmu.TenantId = tm.TenantId AND tmu.ModuleId = tm.ModuleId AND tmu.UserId = tu.UserId
    );
END;

GO

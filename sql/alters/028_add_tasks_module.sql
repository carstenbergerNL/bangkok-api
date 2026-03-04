-- Standalone Tasks Module: table, Module seed, Permissions. Subscription-ready (MaxStandaloneTasks, usage).
-- SQL Server. Run after 027.

-- TasksStandalone table (tenant-scoped; no Project dependency)
IF OBJECT_ID(N'dbo.TasksStandalone', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TasksStandalone
    (
        Id                 UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId           UNIQUEIDENTIFIER NOT NULL,
        Title              NVARCHAR(200)    NOT NULL,
        Description        NVARCHAR(1000)   NULL,
        Status             NVARCHAR(50)     NOT NULL,
        Priority           NVARCHAR(50)     NOT NULL,
        AssignedToUserId   UNIQUEIDENTIFIER NULL,
        CreatedByUserId    UNIQUEIDENTIFIER NOT NULL,
        DueDate            DATETIME2(7)     NULL,
        CreatedAt          DATETIME2(7)     NOT NULL,
        UpdatedAt          DATETIME2(7)     NULL,
        CONSTRAINT FK_TasksStandalone_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TasksStandalone_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.[User](Id) ON DELETE SET NULL,
        CONSTRAINT FK_TasksStandalone_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](Id) ON DELETE NO ACTION,
        CONSTRAINT CK_TasksStandalone_Status CHECK (Status IN (N'Open', N'Completed')),
        CONSTRAINT CK_TasksStandalone_Priority CHECK (Priority IN (N'Low', N'Medium', N'High'))
    );
    CREATE NONCLUSTERED INDEX IX_TasksStandalone_TenantId ON dbo.TasksStandalone (TenantId);
    CREATE NONCLUSTERED INDEX IX_TasksStandalone_AssignedToUserId ON dbo.TasksStandalone (AssignedToUserId);
    CREATE NONCLUSTERED INDEX IX_TasksStandalone_Status ON dbo.TasksStandalone (TenantId, Status);
    CREATE NONCLUSTERED INDEX IX_TasksStandalone_DueDate ON dbo.TasksStandalone (TenantId, DueDate);
END;

-- Seed Module "Tasks"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Module')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Module WHERE [Key] = N'Tasks')
        INSERT INTO dbo.Module (Id, Name, [Key], Description)
        VALUES (NEWID(), N'Tasks', N'Tasks', N'Standalone task management: personal, team, and operational tasks.');
END;

-- Seed Permissions for Tasks module
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Permission')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.View')
        INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.View', N'View standalone tasks.');
    IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Create')
        INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Create', N'Create standalone tasks.');
    IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Edit')
        INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Edit', N'Edit standalone tasks.');
    IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Delete')
        INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Delete', N'Delete standalone tasks.');
    IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Assign')
        INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Assign', N'Assign tasks to users.');
END;

-- Subscription: optional task limit per plan (NULL = unlimited). Free = 100, Pro/Enterprise = unlimited.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.[Plan]') AND name = N'MaxStandaloneTasks')
    ALTER TABLE dbo.[Plan] ADD MaxStandaloneTasks INT NULL;

GO

-- Set Free plan limit (must be in separate batch so column exists when UPDATE is parsed)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.[Plan]') AND name = N'MaxStandaloneTasks')
    UPDATE dbo.[Plan] SET MaxStandaloneTasks = 100 WHERE Name = N'Free' AND MaxStandaloneTasks IS NULL;

GO

-- Usage: track standalone task count per tenant
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TenantUsage')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TenantUsage') AND name = N'StandaloneTasksCount')
    ALTER TABLE dbo.TenantUsage ADD StandaloneTasksCount INT NOT NULL DEFAULT 0;

GO

-- Backfill StandaloneTasksCount (separate batch so column exists when UPDATE is parsed)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TenantUsage') AND name = N'StandaloneTasksCount')
    UPDATE dbo.TenantUsage SET StandaloneTasksCount = (SELECT COUNT(*) FROM dbo.TasksStandalone t WHERE t.TenantId = dbo.TenantUsage.TenantId);

GO

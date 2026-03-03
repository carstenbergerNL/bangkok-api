-- Tenant usage tracking: projects, users, storage, time logs. Optional storage limit on Plan.
-- SQL Server. Run after 024.

IF OBJECT_ID(N'dbo.[TenantUsage]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TenantUsage]
    (
        [TenantId]       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ProjectsCount]  INT              NOT NULL DEFAULT 0,
        [UsersCount]     INT              NOT NULL DEFAULT 0,
        [StorageUsedMB]  DECIMAL(18, 2)   NOT NULL DEFAULT 0,
        [TimeLogsCount]  INT              NOT NULL DEFAULT 0,
        [UpdatedAt]      DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_TenantUsage_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TenantUsage_UpdatedAt] ON dbo.[TenantUsage] ([UpdatedAt]);
END;

-- Optional storage limit per plan (NULL = unlimited)
IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Plan]') AND [name] = N'StorageLimitMB')
    ALTER TABLE dbo.[Plan] ADD [StorageLimitMB] DECIMAL(18, 2) NULL;

-- Backfill TenantUsage for existing tenants (projects, users, storage, time logs)
IF OBJECT_ID(N'dbo.[TenantUsage]', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.[TenantUsage] ([TenantId], [ProjectsCount], [UsersCount], [StorageUsedMB], [TimeLogsCount], [UpdatedAt])
    SELECT t.[Id],
        ISNULL((SELECT COUNT(*) FROM dbo.[Project] p WHERE p.[TenantId] = t.[Id]), 0),
        ISNULL((SELECT COUNT(*) FROM dbo.[TenantUser] tu WHERE tu.[TenantId] = t.[Id]), 0),
        ISNULL((SELECT CAST(SUM(a.[FileSize]) AS DECIMAL(18,2)) / 1024.0 / 1024.0
            FROM dbo.[TaskAttachment] a INNER JOIN dbo.[Task] k ON a.[TaskId] = k.[Id] INNER JOIN dbo.[Project] p ON k.[ProjectId] = p.[Id] WHERE p.[TenantId] = t.[Id]), 0),
        ISNULL((SELECT COUNT(*) FROM dbo.[TaskTimeLog] tl INNER JOIN dbo.[Task] k ON tl.[TaskId] = k.[Id] INNER JOIN dbo.[Project] p ON k.[ProjectId] = p.[Id] WHERE p.[TenantId] = t.[Id]), 0),
        GETUTCDATE()
    FROM dbo.[Tenant] t
    WHERE NOT EXISTS (SELECT 1 FROM dbo.[TenantUsage] u WHERE u.[TenantId] = t.[Id]);
END;

GO

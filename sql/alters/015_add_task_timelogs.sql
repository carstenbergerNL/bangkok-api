-- Add time tracking: TaskTimeLogs table and Task.EstimatedHours for dashboard.
-- Run after 014. Always update 001_initial.sql to reflect full schema.

-- TaskTimeLogs
IF OBJECT_ID(N'dbo.[TaskTimeLog]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskTimeLog]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]      UNIQUEIDENTIFIER NOT NULL,
        [UserId]      UNIQUEIDENTIFIER NOT NULL,
        [Hours]       DECIMAL(5,2)    NOT NULL,
        [Description] NVARCHAR(500)   NULL,
        [CreatedAt]   DATETIME2(7)    NOT NULL,
        CONSTRAINT [FK_TaskTimeLog_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]),
        CONSTRAINT [FK_TaskTimeLog_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskTimeLog_TaskId] ON dbo.[TaskTimeLog] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskTimeLog_UserId] ON dbo.[TaskTimeLog] ([UserId]);
END;

-- Task.EstimatedHours (for dashboard total estimated vs logged, over budget)
IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Task]') AND [name] = N'EstimatedHours')
BEGIN
    ALTER TABLE dbo.[Task] ADD [EstimatedHours] DECIMAL(5,2) NULL;
END;

GO

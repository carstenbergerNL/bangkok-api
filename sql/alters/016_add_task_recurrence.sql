-- Recurring tasks: extend Task with recurrence columns and link to series
-- SQL Server.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Task') AND name = 'IsRecurring')
BEGIN
    ALTER TABLE dbo.Task ADD IsRecurring BIT NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Task') AND name = 'RecurrencePattern')
BEGIN
    ALTER TABLE dbo.Task ADD RecurrencePattern NVARCHAR(100) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Task') AND name = 'RecurrenceInterval')
BEGIN
    ALTER TABLE dbo.Task ADD RecurrenceInterval INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Task') AND name = 'RecurrenceEndDate')
BEGIN
    ALTER TABLE dbo.Task ADD RecurrenceEndDate DATETIME2(7) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Task') AND name = 'RecurrenceSourceTaskId')
BEGIN
    ALTER TABLE dbo.Task ADD RecurrenceSourceTaskId UNIQUEIDENTIFIER NULL;
    ALTER TABLE dbo.Task ADD CONSTRAINT FK_Task_RecurrenceSource FOREIGN KEY (RecurrenceSourceTaskId) REFERENCES dbo.Task(Id) ON DELETE SET NULL;
    CREATE NONCLUSTERED INDEX IX_Task_RecurrenceSourceTaskId ON dbo.Task (RecurrenceSourceTaskId);
END;

GO

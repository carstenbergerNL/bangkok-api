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
    ALTER TABLE dbo.Task ADD CONSTRAINT FK_Task_RecurrenceSource
        FOREIGN KEY (RecurrenceSourceTaskId) REFERENCES dbo.Task(Id) ON DELETE NO ACTION ON UPDATE NO ACTION;
    CREATE NONCLUSTERED INDEX IX_Task_RecurrenceSourceTaskId ON dbo.Task (RecurrenceSourceTaskId);
END;
ELSE IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Task_RecurrenceSource')
BEGIN
    -- Column exists from a previous run; add constraint and index only (ON DELETE NO ACTION avoids cascade path error)
    ALTER TABLE dbo.Task ADD CONSTRAINT FK_Task_RecurrenceSource
        FOREIGN KEY (RecurrenceSourceTaskId) REFERENCES dbo.Task(Id) ON DELETE NO ACTION ON UPDATE NO ACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Task_RecurrenceSourceTaskId' AND object_id = OBJECT_ID(N'dbo.Task'))
        CREATE NONCLUSTERED INDEX IX_Task_RecurrenceSourceTaskId ON dbo.Task (RecurrenceSourceTaskId);
END;

GO

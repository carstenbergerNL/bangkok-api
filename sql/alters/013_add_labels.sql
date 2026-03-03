-- Task Labels: Labels (project-level) and TaskLabels (task-label many-to-many)
-- SQL Server. No cascade delete. Run after project/task tables exist.

IF OBJECT_ID(N'dbo.Label', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Label
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name      NVARCHAR(100)    NOT NULL,
        Color     NVARCHAR(20)     NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_Label_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id)
    );
    CREATE NONCLUSTERED INDEX IX_Label_ProjectId ON dbo.Label (ProjectId);
END;

IF OBJECT_ID(N'dbo.TaskLabel', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskLabel
    (
        Id      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId  UNIQUEIDENTIFIER NOT NULL,
        LabelId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_TaskLabel_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id),
        CONSTRAINT FK_TaskLabel_Label FOREIGN KEY (LabelId) REFERENCES dbo.Label(Id),
        CONSTRAINT UQ_TaskLabel_TaskId_LabelId UNIQUE (TaskId, LabelId)
    );
    CREATE NONCLUSTERED INDEX IX_TaskLabel_TaskId ON dbo.TaskLabel (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskLabel_LabelId ON dbo.TaskLabel (LabelId);
END;

GO

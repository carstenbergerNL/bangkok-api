-- Task Comments and Activity Logging
-- SQL Server. No cascade delete. Foreign keys to dbo.Task and dbo.[User].

-- TaskComments
IF OBJECT_ID(N'dbo.TaskComment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskComment
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId    UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Content   NVARCHAR(2000)   NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        UpdatedAt DATETIME2(7)     NULL,
        CONSTRAINT FK_TaskComment_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id),
        CONSTRAINT FK_TaskComment_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskComment_TaskId ON dbo.TaskComment (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskComment_UserId ON dbo.TaskComment (UserId);
END;

-- TaskActivities
IF OBJECT_ID(N'dbo.TaskActivity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskActivity
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId    UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Action    NVARCHAR(100)    NOT NULL,
        OldValue  NVARCHAR(1000)   NULL,
        NewValue  NVARCHAR(1000)   NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_TaskActivity_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id),
        CONSTRAINT FK_TaskActivity_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskActivity_TaskId ON dbo.TaskActivity (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskActivity_CreatedAt ON dbo.TaskActivity (CreatedAt DESC);
END;

GO

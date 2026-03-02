-- Project Planning module: Projects and Tasks tables
-- SQL Server. No cascade delete. Foreign keys to dbo.[User].

-- Projects
IF OBJECT_ID(N'dbo.Project', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Project
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name              NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NULL,
        Status            NVARCHAR(50)     NOT NULL,
        CreatedByUserId   UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)     NOT NULL,
        UpdatedAt         DATETIME2(7)     NULL,
        CONSTRAINT FK_Project_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Project_CreatedByUserId ON dbo.Project (CreatedByUserId);
    CREATE NONCLUSTERED INDEX IX_Project_Status ON dbo.Project (Status);
END;

-- Tasks
IF OBJECT_ID(N'dbo.Task', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Task
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId         UNIQUEIDENTIFIER NOT NULL,
        Title             NVARCHAR(200)    NOT NULL,
        Description       NVARCHAR(1000)   NULL,
        Status            NVARCHAR(50)     NOT NULL,
        Priority          NVARCHAR(50)     NOT NULL,
        AssignedToUserId  UNIQUEIDENTIFIER NULL,
        DueDate           DATETIME2(7)     NULL,
        CreatedByUserId   UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)     NOT NULL,
        UpdatedAt         DATETIME2(7)     NULL,
        CONSTRAINT FK_Task_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id),
        CONSTRAINT FK_Task_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_Task_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Task_ProjectId ON dbo.Task (ProjectId);
    CREATE NONCLUSTERED INDEX IX_Task_AssignedToUserId ON dbo.Task (AssignedToUserId);
    CREATE NONCLUSTERED INDEX IX_Task_Status ON dbo.Task (Status);
END;

GO

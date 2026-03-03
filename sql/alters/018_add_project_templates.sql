-- Project templates and template tasks for "create project from template".
-- SQL Server.

IF OBJECT_ID(N'dbo.ProjectTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTemplate
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name        NVARCHAR(200)   NOT NULL,
        Description NVARCHAR(500)   NULL,
        CreatedAt   DATETIME2(7)    NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.ProjectTemplateTask', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTemplateTask
    (
        Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TemplateId     UNIQUEIDENTIFIER NOT NULL,
        Title          NVARCHAR(200)   NOT NULL,
        Description    NVARCHAR(1000)  NULL,
        DefaultStatus  NVARCHAR(50)    NULL,
        DefaultPriority NVARCHAR(50)   NULL,
        CONSTRAINT FK_ProjectTemplateTask_Template FOREIGN KEY (TemplateId) REFERENCES dbo.ProjectTemplate(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_ProjectTemplateTask_TemplateId ON dbo.ProjectTemplateTask (TemplateId);
END;

GO

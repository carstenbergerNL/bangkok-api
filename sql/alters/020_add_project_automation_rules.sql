-- Project automation rules (lightweight: trigger + action, no workflow engine).
-- SQL Server.

IF OBJECT_ID(N'dbo.ProjectAutomationRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectAutomationRule
    (
        Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId     UNIQUEIDENTIFIER NOT NULL,
        [Trigger]    NVARCHAR(100)    NOT NULL,
        [Action]       NVARCHAR(100)    NOT NULL,
        TargetUserId  UNIQUEIDENTIFIER NULL,
        TargetValue   NVARCHAR(100)    NULL,
        CONSTRAINT FK_ProjectAutomationRule_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ProjectAutomationRule_TargetUser FOREIGN KEY (TargetUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_ProjectAutomationRule_ProjectId ON dbo.ProjectAutomationRule (ProjectId);
END;

GO

-- Add ProjectMembers table for project-level access control.
-- Run after 011. Always update 001_initial.sql to reflect full schema.

IF OBJECT_ID(N'dbo.ProjectMember', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectMember
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Role      NVARCHAR(50)     NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_ProjectMember_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id),
        CONSTRAINT FK_ProjectMember_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id),
        CONSTRAINT UQ_ProjectMember_ProjectId_UserId UNIQUE (ProjectId, UserId)
    );
    CREATE NONCLUSTERED INDEX IX_ProjectMember_ProjectId ON dbo.ProjectMember (ProjectId);
    CREATE NONCLUSTERED INDEX IX_ProjectMember_UserId ON dbo.ProjectMember (UserId);
END;

GO

-- Project custom fields and task custom field values.
-- SQL Server. No dynamic schema; values stored as NVARCHAR(MAX).

IF OBJECT_ID(N'dbo.ProjectCustomField', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectCustomField
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId   UNIQUEIDENTIFIER NOT NULL,
        Name        NVARCHAR(100)   NOT NULL,
        FieldType   NVARCHAR(50)    NOT NULL,
        Options     NVARCHAR(MAX)   NULL,
        CreatedAt   DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_ProjectCustomField_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_ProjectCustomField_ProjectId ON dbo.ProjectCustomField (ProjectId);
END;

IF OBJECT_ID(N'dbo.TaskCustomFieldValue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskCustomFieldValue
    (
        Id       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId   UNIQUEIDENTIFIER NOT NULL,
        FieldId  UNIQUEIDENTIFIER NOT NULL,
        Value    NVARCHAR(MAX)   NULL,
        CONSTRAINT FK_TaskCustomFieldValue_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TaskCustomFieldValue_Field FOREIGN KEY (FieldId) REFERENCES dbo.ProjectCustomField(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_TaskCustomFieldValue_TaskId ON dbo.TaskCustomFieldValue (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskCustomFieldValue_FieldId ON dbo.TaskCustomFieldValue (FieldId);
END;

GO

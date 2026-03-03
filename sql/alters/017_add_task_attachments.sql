-- Task file attachments: metadata in DB, files on disk under /uploads/tasks/{taskId}/
-- SQL Server.

IF OBJECT_ID(N'dbo.TaskAttachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskAttachment
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId            UNIQUEIDENTIFIER NOT NULL,
        FileName          NVARCHAR(255)   NOT NULL,
        FilePath          NVARCHAR(500)   NOT NULL,
        FileSize          INT             NOT NULL,
        ContentType       NVARCHAR(100)   NOT NULL,
        UploadedByUserId  UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_TaskAttachment_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TaskAttachment_User FOREIGN KEY (UploadedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskAttachment_TaskId ON dbo.TaskAttachment (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskAttachment_UploadedByUserId ON dbo.TaskAttachment (UploadedByUserId);
END;

GO

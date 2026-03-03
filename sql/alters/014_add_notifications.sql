-- Add Notifications table for in-app user notifications.
-- Run after 013. Always update 001_initial.sql to reflect full schema.

IF OBJECT_ID(N'dbo.Notification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notification
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId      UNIQUEIDENTIFIER NOT NULL,
        Type        NVARCHAR(100)    NOT NULL,
        Title       NVARCHAR(200)    NOT NULL,
        Message     NVARCHAR(1000)   NOT NULL,
        ReferenceId UNIQUEIDENTIFIER NULL,
        IsRead      BIT              NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_Notification_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Notification_UserId ON dbo.Notification (UserId);
    CREATE NONCLUSTERED INDEX IX_Notification_UserId_IsRead_CreatedAt ON dbo.Notification (UserId, IsRead, CreatedAt DESC);
END;

GO

-- Add soft delete columns to User (backward compatible).
-- Always update 001_initial.sql to reflect full schema; keep this alter for history.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'IsDeleted'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD IsDeleted BIT NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'DeletedAt'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD DeletedAt DATETIME2(7) NULL;
END;

GO

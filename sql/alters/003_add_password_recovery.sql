-- Add password recovery columns to User (backward compatible).
-- Always update 001_initial.sql to reflect full schema; keep this alter for history.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'RecoverString'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD RecoverString NVARCHAR(200) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'RecoverStringExpiry'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD RecoverStringExpiry DATETIME2(7) NULL;
END;

GO

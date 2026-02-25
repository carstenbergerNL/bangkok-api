-- Example alter script: add optional DisplayName to User (backward compatible)
-- Always update 001_initial.sql to reflect full schema; keep this alter for history.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'DisplayName'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD DisplayName NVARCHAR(256) NULL;
END;

GO

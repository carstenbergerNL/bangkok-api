-- Add account lockout columns to User (backward compatible).
-- After 5 failed login attempts, account is locked for 15 minutes.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'FailedLoginAttempts'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD FailedLoginAttempts INT NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'LockoutEnd'
)
BEGIN
    ALTER TABLE dbo.[User]
    ADD LockoutEnd DATETIME2(7) NULL;
END;

GO

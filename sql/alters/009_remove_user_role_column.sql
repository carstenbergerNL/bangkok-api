-- Migrate User.Role to UserRole, then drop User.Role column.
-- Run after 008_add_role_management.sql and seed. Roles table must exist.

-- 1) Ensure every user has a UserRole from their current User.Role
INSERT INTO dbo.UserRole (Id, UserId, RoleId, CreatedAt)
SELECT NEWID(), u.Id, r.Id, GETUTCDATE()
FROM dbo.[User] u
INNER JOIN dbo.Role r ON r.Name = u.Role
WHERE NOT EXISTS (SELECT 1 FROM dbo.UserRole ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

-- 2) Drop the Role column
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = 'Role'
)
BEGIN
    ALTER TABLE dbo.[User] DROP COLUMN Role;
END;

GO

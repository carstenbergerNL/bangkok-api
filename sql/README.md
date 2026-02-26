# SQL schema and migrations

Database scripts for the **Bangkok API**. Target: **SQL Server**. Use `UNIQUEIDENTIFIER` for primary keys, `DATETIME2` for dates, `NVARCHAR` for strings.

## Contents

| File / folder | Purpose |
|---------------|---------|
| **001_initial.sql** | Full initial schema. Creates `dbo.[User]` and `dbo.RefreshToken` with all columns in their current shape. Run this on a new database. |
| **alters/** | Incremental migration scripts. Add columns or objects in order (002, 003, …). Each script is idempotent (checks before adding). |

## Tables (current schema)

- **dbo.[User]** – Users: Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd. Unique constraint on Email.
- **dbo.RefreshToken** – Refresh tokens: Id, UserId (FK to User), Token, ExpiresAtUtc, CreatedAtUtc, RevokedReason, RevokedAtUtc.
- **dbo.Profile** – User profile (1:1 with User): Id, UserId (FK, unique), FirstName, MiddleName, LastName, DateOfBirth, PhoneNumber, AvatarBase64, CreatedAtUtc, UpdatedAtUtc.

## Alter scripts (order)

| Script | Change |
|--------|--------|
| 002_add_user_display_name.sql | Add optional `DisplayName` to User |
| 003_add_password_recovery.sql | Add `RecoverString`, `RecoverStringExpiry` to User |
| 004_add_user_is_active.sql | Add `IsActive` to User |
| 005_add_soft_delete_to_users.sql | Add `IsDeleted`, `DeletedAt` to User |
| 006_add_lockout_columns.sql | Add `FailedLoginAttempts`, `LockoutEnd` to User |
| 007_add_profiles_table.sql | Add `Profile` table (1:1 with User) |

## Conventions

- **New database:** Run `001_initial.sql` only (it already reflects the full schema including columns added by alters).
- **Existing database:** Run any alters you haven’t applied yet, in numeric order. Scripts use `IF NOT EXISTS` (or equivalent) so they are safe to run multiple times.
- **After adding a new alter:** Update `001_initial.sql` so it continues to represent the full current schema for new installs. Keep the alter script for migration history.

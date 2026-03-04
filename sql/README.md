# SQL schema and migrations

Database scripts for the **Bangkok API**. Target: **SQL Server**. Use `UNIQUEIDENTIFIER` for primary keys, `DATETIME2` for dates, `NVARCHAR` for strings.

## Contents

| File / folder | Purpose |
|---------------|---------|
| **001_initial.sql** | Full initial schema. Creates core tables (User, RefreshToken, Role, UserRole, Permission, RolePermission, Profile, Project, Task, TaskComment, TaskActivity, ProjectMember, Label, TaskLabel). Run this on a new database. |
| **alters/** | Incremental migration scripts. Add columns or objects in order (002, 003, …). Each script is idempotent (checks before adding). |

## Tables (current schema)

- **dbo.[User]** – Users: Id, Email, DisplayName, PasswordHash, PasswordSalt, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd. Unique constraint on Email.
- **dbo.RefreshToken** – Refresh tokens: Id, UserId (FK to User), Token, ExpiresAtUtc, CreatedAtUtc, RevokedReason, RevokedAtUtc.
- **dbo.Role** – Roles: Id, Name, Description, CreatedAt. Unique on Name.
- **dbo.UserRole** – User–Role many-to-many: Id, UserId, RoleId, CreatedAt. FK to User and Role.
- **dbo.Permission** – Permissions: Id, Name, Description. Unique on Name.
- **dbo.RolePermission** – Role–Permission many-to-many: Id, RoleId, PermissionId. FK to Role and Permission.
- **dbo.Profile** – User profile (1:1 with User): Id, UserId (FK, unique), FirstName, MiddleName, LastName, DateOfBirth, PhoneNumber, AvatarBase64, CreatedAtUtc, UpdatedAtUtc.
- **dbo.Project** – Projects: Id, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt.
- **dbo.ProjectMember** – Project members: Id, ProjectId, UserId, Role (e.g. Owner, Member, Viewer), CreatedAt. Unique (ProjectId, UserId).
- **dbo.Task** – Tasks: Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, **EstimatedHours**, CreatedByUserId, CreatedAt, UpdatedAt.
- **dbo.TaskComment** – Task comments: Id, TaskId, UserId, Content, CreatedAt, UpdatedAt.
- **dbo.TaskActivity** – Task activity log: Id, TaskId, UserId, Action, OldValue, NewValue, CreatedAt.
- **dbo.TaskTimeLog** – Time tracking: Id, TaskId, UserId, Hours (DECIMAL(5,2)), Description, CreatedAt.
- **dbo.Label** – Project-level labels: Id, Name, Color, ProjectId, CreatedAt.
- **dbo.TaskLabel** – Task–Label many-to-many: Id, TaskId, LabelId.
- **dbo.Notification** – User notifications: Id, UserId, Type, Title, Message, ReferenceId, IsRead, CreatedAt.
- **dbo.ProjectAutomationRule** – Automation rules: Id, ProjectId, Trigger, Action, TargetUserId, TargetValue. Triggers: TaskCompleted, TaskOverdue, TaskAssigned. Actions: NotifyUser, ChangeStatus, AddLabel.

## Alter scripts (order)

| Script | Change |
|--------|--------|
| 002_add_user_display_name.sql | Add optional `DisplayName` to User |
| 003_add_password_recovery.sql | Add `RecoverString`, `RecoverStringExpiry` to User |
| 004_add_user_is_active.sql | Add `IsActive` to User |
| 005_add_soft_delete_to_users.sql | Add `IsDeleted`, `DeletedAt` to User |
| 006_add_lockout_columns.sql | Add `FailedLoginAttempts`, `LockoutEnd` to User |
| 007_add_profiles_table.sql | Add `Profile` table (1:1 with User) |
| 008_add_role_management.sql | Add Role, UserRole, Permission, RolePermission |
| 009_remove_user_role_column.sql | Migrate User.Role to UserRole, then drop User.Role |
| 010_add_project_module.sql | Add Project and Task tables |
| 011_add_task_comments_activity.sql | Add TaskComment and TaskActivity |
| 012_add_project_members.sql | Add ProjectMember |
| 013_add_labels.sql | Add Label and TaskLabel |
| 014_add_notifications.sql | Add Notification |
| 015_add_task_timelogs.sql | Add TaskTimeLog, add Task.EstimatedHours |
| 016_add_task_recurrence.sql | Add task recurrence columns |
| 017_add_task_attachments.sql | Add TaskAttachment, AttachmentsController |
| 018_add_project_templates.sql | Add ProjectTemplate, ProjectTemplateTask |
| 019_add_project_custom_fields.sql | Add ProjectCustomField, TaskCustomFieldValue |
| 020_add_project_automation_rules.sql | Add ProjectAutomationRule (trigger/action rules) |
| 027_add_user_module_access.sql | Add TenantModuleUser (user-level module access; Tenant Admin grant/revoke) |
| 028_add_tasks_module.sql | Add TasksStandalone table, Module "Tasks", permissions (Tasks.View/Create/Edit/Delete/Assign), Plan.MaxStandaloneTasks, TenantUsage.StandaloneTasksCount |

## Conventions

- **New database:** Run `001_initial.sql` only (it already reflects the full schema including columns added by alters).
- **Existing database:** Run any alters you haven’t applied yet, in numeric order. Scripts use `IF NOT EXISTS` (or equivalent) so they are safe to run multiple times.
- **After adding a new alter:** Update `001_initial.sql` so it continues to represent the full current schema for new installs. Keep the alter script for migration history.

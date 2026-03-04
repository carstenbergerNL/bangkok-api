-- Seed permissions for Project, Task (project tasks), and Tasks (standalone module).
-- Run against your database when Permission table exists. Idempotent: only inserts if name is missing.
-- Optionally assigns all to Admin role (if Admin role exists).

-- Project & Task (project-based tasks)
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Project.View')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Project.View', N'View projects');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Project.Create')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Project.Create', N'Create projects');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Project.Edit')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Project.Edit', N'Edit projects');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Project.Delete')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Project.Delete', N'Delete projects');

IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.View')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.View', N'View tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.Create')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.Create', N'Create tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.Edit')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.Edit', N'Edit tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.Delete')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.Delete', N'Delete tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.Assign')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.Assign', N'Assign tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.Comment')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.Comment', N'Comment on tasks');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Task.ViewActivity')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Task.ViewActivity', N'View task activity');

-- Standalone Tasks module
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.View')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.View', N'View standalone tasks.');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Create')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Create', N'Create standalone tasks.');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Edit')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Edit', N'Edit standalone tasks.');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Delete')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Delete', N'Delete standalone tasks.');
IF NOT EXISTS (SELECT 1 FROM dbo.[Permission] WHERE [Name] = N'Tasks.Assign')
    INSERT INTO dbo.[Permission] (Id, [Name], [Description]) VALUES (NEWID(), N'Tasks.Assign', N'Assign standalone tasks.');

GO

-- Assign all above permissions to Admin role (if Admin exists and not already assigned)
INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId)
SELECT NEWID(), r.Id, p.Id
FROM dbo.[Role] r
CROSS JOIN dbo.[Permission] p
WHERE r.[Name] = N'Admin'
  AND p.[Name] IN (
    N'Project.View', N'Project.Create', N'Project.Edit', N'Project.Delete',
    N'Task.View', N'Task.Create', N'Task.Edit', N'Task.Delete', N'Task.Assign', N'Task.Comment', N'Task.ViewActivity',
    N'Tasks.View', N'Tasks.Create', N'Tasks.Edit', N'Tasks.Delete', N'Tasks.Assign'
  )
  AND NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

GO

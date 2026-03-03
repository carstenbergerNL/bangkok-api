using System.Data;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class RoleSeedRunner
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<RoleSeedRunner> _logger;

    public RoleSeedRunner(ISqlConnectionFactory connectionFactory, ILogger<RoleSeedRunner> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();

            var adminRole = await connection.QuerySingleOrDefaultAsync<Role>(
                new CommandDefinition("SELECT Id, Name, Description, CreatedAt FROM dbo.Role WHERE Name = N'Admin'", cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (adminRole != null)
            {
                var roleColumnExists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                    "SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = N'Role'", cancellationToken: cancellationToken)).ConfigureAwait(false);
                if (roleColumnExists == 1)
                    await AssignAdminRoleToLegacyAdminsAsync(connection, adminRole.Id, cancellationToken).ConfigureAwait(false);
                await EnsureViewAdminSettingsPermissionAsync(connection, adminRole.Id, cancellationToken).ConfigureAwait(false);
                await EnsureProjectAndTaskPermissionsAsync(connection, adminRole.Id, cancellationToken).ConfigureAwait(false);
                await EnsureSuperAdminRoleAsync(connection, cancellationToken).ConfigureAwait(false);
                return;
            }

            var adminId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var manageUsersId = Guid.NewGuid();
            var manageRolesId = Guid.NewGuid();
            var viewAdminSettingsId = Guid.NewGuid();
            var projectViewId = Guid.NewGuid();
            var projectCreateId = Guid.NewGuid();
            var projectEditId = Guid.NewGuid();
            var projectDeleteId = Guid.NewGuid();
            var taskViewId = Guid.NewGuid();
            var taskCreateId = Guid.NewGuid();
            var taskEditId = Guid.NewGuid();
            var taskDeleteId = Guid.NewGuid();
            var taskAssignId = Guid.NewGuid();
            var taskCommentId = Guid.NewGuid();
            var taskViewActivityId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.Role (Id, Name, Description, CreatedAt) VALUES
                (@AdminId, N'Admin', N'Administrator role', @Now),
                (@UserId, N'User', N'Standard user role', @Now)",
                new { AdminId = adminId, UserId = userId, Now = now },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.Permission (Id, Name, Description) VALUES
                (@ManageUsersId, N'ManageUsers', N'Manage users'),
                (@ManageRolesId, N'ManageRoles', N'Manage roles'),
                (@ViewAdminSettingsId, N'ViewAdminSettings', N'View and access Admin Settings'),
                (@ProjectViewId, N'Project.View', N'View projects'),
                (@ProjectCreateId, N'Project.Create', N'Create projects'),
                (@ProjectEditId, N'Project.Edit', N'Edit projects'),
                (@ProjectDeleteId, N'Project.Delete', N'Delete projects'),
                (@TaskViewId, N'Task.View', N'View tasks'),
                (@TaskCreateId, N'Task.Create', N'Create tasks'),
                (@TaskEditId, N'Task.Edit', N'Edit tasks'),
                (@TaskDeleteId, N'Task.Delete', N'Delete tasks'),
                (@TaskAssignId, N'Task.Assign', N'Assign tasks'),
                (@TaskCommentId, N'Task.Comment', N'Comment on tasks'),
                (@TaskViewActivityId, N'Task.ViewActivity', N'View task activity')",
                new { ManageUsersId = manageUsersId, ManageRolesId = manageRolesId, ViewAdminSettingsId = viewAdminSettingsId,
                    ProjectViewId = projectViewId, ProjectCreateId = projectCreateId, ProjectEditId = projectEditId, ProjectDeleteId = projectDeleteId,
                    TaskViewId = taskViewId, TaskCreateId = taskCreateId, TaskEditId = taskEditId, TaskDeleteId = taskDeleteId, TaskAssignId = taskAssignId,
                    TaskCommentId = taskCommentId, TaskViewActivityId = taskViewActivityId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId) VALUES
                (NEWID(), @AdminId, @ManageUsersId),
                (NEWID(), @AdminId, @ManageRolesId),
                (NEWID(), @AdminId, @ViewAdminSettingsId),
                (NEWID(), @AdminId, @ProjectViewId),
                (NEWID(), @AdminId, @ProjectCreateId),
                (NEWID(), @AdminId, @ProjectEditId),
                (NEWID(), @AdminId, @ProjectDeleteId),
                (NEWID(), @AdminId, @TaskViewId),
                (NEWID(), @AdminId, @TaskCreateId),
                (NEWID(), @AdminId, @TaskEditId),
                (NEWID(), @AdminId, @TaskDeleteId),
                (NEWID(), @AdminId, @TaskAssignId),
                (NEWID(), @AdminId, @TaskCommentId),
                (NEWID(), @AdminId, @TaskViewActivityId)",
                new { AdminId = adminId, ManageUsersId = manageUsersId, ManageRolesId = manageRolesId, ViewAdminSettingsId = viewAdminSettingsId,
                    ProjectViewId = projectViewId, ProjectCreateId = projectCreateId, ProjectEditId = projectEditId, ProjectDeleteId = projectDeleteId,
                    TaskViewId = taskViewId, TaskCreateId = taskCreateId, TaskEditId = taskEditId, TaskDeleteId = taskDeleteId, TaskAssignId = taskAssignId,
                    TaskCommentId = taskCommentId, TaskViewActivityId = taskViewActivityId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            _logger.LogInformation("Role management seed completed. Roles: Admin, User. Permissions: ManageUsers, ManageRoles, ViewAdminSettings, Project.*, Task.*.");

            await AssignAdminRoleToLegacyAdminsAsync(connection, adminId, cancellationToken).ConfigureAwait(false);
            await EnsureSuperAdminRoleAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task EnsureSuperAdminRoleAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var existing = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT Id FROM dbo.[Role] WHERE Name = N'SuperAdmin'", cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (existing.HasValue) return;

        var superAdminId = Guid.NewGuid();
        var platformAdminPermId = Guid.NewGuid();
        await connection.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO dbo.[Role] (Id, Name, Description, CreatedAt) VALUES (@Id, N'SuperAdmin', N'Platform administrator; access to platform dashboard.', GETUTCDATE())",
            new { Id = superAdminId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO dbo.[Permission] (Id, Name, Description) VALUES (@Id, N'PlatformAdmin', N'Access platform admin dashboard and tenant management')",
            new { Id = platformAdminPermId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO dbo.[RolePermission] (Id, RoleId, PermissionId) VALUES (NEWID(), @RoleId, @PermissionId)",
            new { RoleId = superAdminId, PermissionId = platformAdminPermId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task EnsureViewAdminSettingsPermissionAsync(IDbConnection connection, Guid adminRoleId, CancellationToken cancellationToken)
    {
        var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT Id FROM dbo.Permission WHERE Name = N'ViewAdminSettings'", cancellationToken: cancellationToken)).ConfigureAwait(false);
        var viewAdminSettingsId = existingId ?? Guid.NewGuid();
        if (!existingId.HasValue)
        {
            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.Permission (Id, Name, Description) VALUES (@Id, N'ViewAdminSettings', N'View and access Admin Settings')",
                new { Id = viewAdminSettingsId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        var assigned = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM dbo.RolePermission WHERE RoleId = @RoleId AND PermissionId = @PermissionId",
            new { RoleId = adminRoleId, PermissionId = viewAdminSettingsId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (assigned != 1)
        {
            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId) VALUES (NEWID(), @RoleId, @PermissionId)",
                new { RoleId = adminRoleId, PermissionId = viewAdminSettingsId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    private static async Task EnsureProjectAndTaskPermissionsAsync(IDbConnection connection, Guid adminRoleId, CancellationToken cancellationToken)
    {
        var permissions = new[] { ("Project.View", "View projects"), ("Project.Create", "Create projects"), ("Project.Edit", "Edit projects"), ("Project.Delete", "Delete projects"),
            ("Task.View", "View tasks"), ("Task.Create", "Create tasks"), ("Task.Edit", "Edit tasks"), ("Task.Delete", "Delete tasks"), ("Task.Assign", "Assign tasks"),
            ("Task.Comment", "Comment on tasks"), ("Task.ViewActivity", "View task activity") };

        foreach (var (name, description) in permissions)
        {
            var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT Id FROM dbo.Permission WHERE Name = @Name", new { Name = name }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            var permId = existingId ?? Guid.NewGuid();
            if (!existingId.HasValue)
            {
                await connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO dbo.Permission (Id, Name, Description) VALUES (@Id, @Name, @Description)",
                    new { Id = permId, Name = name, Description = description },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var assigned = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                "SELECT 1 FROM dbo.RolePermission WHERE RoleId = @RoleId AND PermissionId = @PermissionId",
                new { RoleId = adminRoleId, PermissionId = permId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (assigned != 1)
            {
                await connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId) VALUES (NEWID(), @RoleId, @PermissionId)",
                    new { RoleId = adminRoleId, PermissionId = permId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
        }
    }

    private static async Task AssignAdminRoleToLegacyAdminsAsync(IDbConnection connection, Guid adminRoleId, CancellationToken cancellationToken)
    {
        var adminUsers = (await connection.QueryAsync<(Guid Id, string Email)>(
            new CommandDefinition("SELECT Id, Email FROM dbo.[User] WHERE Role = N'Admin' AND IsDeleted = 0", cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        foreach (var (userId, _) in adminUsers)
        {
            var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                "SELECT 1 FROM dbo.UserRole WHERE UserId = @UserId AND RoleId = @RoleId",
                new { UserId = userId, RoleId = adminRoleId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (exists != 1)
            {
                await connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO dbo.UserRole (Id, UserId, RoleId, CreatedAt) VALUES (NEWID(), @UserId, @RoleId, GETUTCDATE())",
                    new { UserId = userId, RoleId = adminRoleId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
        }
    }
}

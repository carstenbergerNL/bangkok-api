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
                return;
            }

            var adminId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var manageUsersId = Guid.NewGuid();
            var manageRolesId = Guid.NewGuid();
            var viewAdminSettingsId = Guid.NewGuid();
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
                (@ViewAdminSettingsId, N'ViewAdminSettings', N'View and access Admin Settings')",
                new { ManageUsersId = manageUsersId, ManageRolesId = manageRolesId, ViewAdminSettingsId = viewAdminSettingsId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId) VALUES
                (NEWID(), @AdminId, @ManageUsersId),
                (NEWID(), @AdminId, @ManageRolesId),
                (NEWID(), @AdminId, @ViewAdminSettingsId)",
                new { AdminId = adminId, ManageUsersId = manageUsersId, ManageRolesId = manageRolesId, ViewAdminSettingsId = viewAdminSettingsId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            _logger.LogInformation("Role management seed completed. Roles: Admin, User. Permissions: ManageUsers, ManageRoles, ViewAdminSettings.");

            await AssignAdminRoleToLegacyAdminsAsync(connection, adminId, cancellationToken).ConfigureAwait(false);
        }
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

using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RolePermissionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT p.Id, p.Name, p.Description
                FROM dbo.Permission p
                INNER JOIN dbo.RolePermission rp ON rp.PermissionId = p.Id
                WHERE rp.RoleId = @RoleId
                ORDER BY p.Name";
            var items = await connection.QueryAsync<Permission>(new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<bool> ExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT 1 FROM dbo.RolePermission WHERE RoleId = @RoleId AND PermissionId = @PermissionId";
            var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { RoleId = roleId, PermissionId = permissionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return exists.HasValue && exists.Value == 1;
        }
    }

    public async Task AssignAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.RolePermission (Id, RoleId, PermissionId)
                VALUES (NEWID(), @RoleId, @PermissionId)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { RoleId = roleId, PermissionId = permissionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RemoveAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.RolePermission WHERE RoleId = @RoleId AND PermissionId = @PermissionId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { RoleId = roleId, PermissionId = permissionId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

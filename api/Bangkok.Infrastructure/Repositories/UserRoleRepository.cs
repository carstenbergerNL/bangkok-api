using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRoleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT r.Id, r.Name, r.Description, r.CreatedAt
                FROM dbo.Role r
                INNER JOIN dbo.UserRole ur ON ur.RoleId = r.Id
                WHERE ur.UserId = @UserId
                ORDER BY r.Name";
            var items = await connection.QueryAsync<Role>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT 1 FROM dbo.UserRole WHERE UserId = @UserId AND RoleId = @RoleId";
            var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { UserId = userId, RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return exists.HasValue && exists.Value == 1;
        }
    }

    public async Task AssignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.UserRole (Id, UserId, RoleId, CreatedAt)
                VALUES (NEWID(), @UserId, @RoleId, GETUTCDATE())";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.UserRole WHERE UserId = @UserId AND RoleId = @RoleId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.UserRole WHERE UserId = @UserId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

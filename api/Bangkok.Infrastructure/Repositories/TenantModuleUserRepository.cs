using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantModuleUserRepository : ITenantModuleUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantModuleUserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> ExistsAsync(Guid tenantId, Guid moduleId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.TenantModuleUser
                    WHERE TenantId = @TenantId AND ModuleId = @ModuleId AND UserId = @UserId
                ) THEN 1 ELSE 0 END AS BIT)";
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { TenantId = tenantId, ModuleId = moduleId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsWithAccessAsync(Guid tenantId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT UserId FROM dbo.TenantModuleUser WHERE TenantId = @TenantId AND ModuleId = @ModuleId ORDER BY UserId";
            var list = await connection.QueryAsync<Guid>(
                new CommandDefinition(sql, new { TenantId = tenantId, ModuleId = moduleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    /// <summary>
    /// Returns module keys the user can access: module active for tenant AND (no user-level list for that module OR user in list).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetActiveModuleKeysForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT m.[Key]
                FROM dbo.TenantModule tm
                INNER JOIN dbo.[Module] m ON m.Id = tm.ModuleId
                WHERE tm.TenantId = @TenantId AND tm.IsActive = 1
                AND (
                    NOT EXISTS (SELECT 1 FROM dbo.TenantModuleUser tmu WHERE tmu.TenantId = tm.TenantId AND tmu.ModuleId = tm.ModuleId)
                    OR EXISTS (SELECT 1 FROM dbo.TenantModuleUser tmu WHERE tmu.TenantId = tm.TenantId AND tmu.ModuleId = tm.ModuleId AND tmu.UserId = @UserId)
                )
                ORDER BY m.Name";
            var keys = await connection.QueryAsync<string>(
                new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return keys.ToList();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetModuleIdsForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT ModuleId FROM dbo.TenantModuleUser WHERE TenantId = @TenantId AND UserId = @UserId ORDER BY ModuleId";
            var list = await connection.QueryAsync<Guid>(
                new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task RemoveAllForUserInTenantAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TenantModuleUser WHERE TenantId = @TenantId AND UserId = @UserId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> AddAsync(TenantModuleUser entity, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.TenantModuleUser (Id, TenantId, ModuleId, UserId, CreatedAt)
                VALUES (@Id, @TenantId, @ModuleId, @UserId, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                entity.Id,
                entity.TenantId,
                entity.ModuleId,
                entity.UserId,
                entity.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return entity.Id;
        }
    }

    public async Task<bool> RemoveAsync(Guid tenantId, Guid moduleId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TenantModuleUser WHERE TenantId = @TenantId AND ModuleId = @ModuleId AND UserId = @UserId";
            var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, ModuleId = moduleId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return rows > 0;
        }
    }
}

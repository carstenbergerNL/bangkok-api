using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantUserRepository : ITenantUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantUserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TenantUser>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TenantId, UserId, Role, CreatedAt FROM dbo.TenantUser WHERE UserId = @UserId ORDER BY CreatedAt";
            var list = await connection.QueryAsync<TenantUser>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<TenantUser?> GetByTenantAndUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TenantId, UserId, Role, CreatedAt FROM dbo.TenantUser WHERE TenantId = @TenantId AND UserId = @UserId";
            return await connection.QuerySingleOrDefaultAsync<TenantUser>(
                new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TenantId, UserId, Role, CreatedAt FROM dbo.TenantUser WHERE TenantId = @TenantId ORDER BY CreatedAt";
            var list = await connection.QueryAsync<TenantUser>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(TenantUser tenantUser, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "INSERT INTO dbo.TenantUser (Id, TenantId, UserId, Role, CreatedAt) VALUES (@Id, @TenantId, @UserId, @Role, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { tenantUser.Id, tenantUser.TenantId, tenantUser.UserId, tenantUser.Role, tenantUser.CreatedAt }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return tenantUser.Id;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TenantUser WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

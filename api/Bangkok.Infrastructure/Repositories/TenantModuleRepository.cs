using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantModuleRepository : ITenantModuleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantModuleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantModule?> GetAsync(Guid tenantId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TenantId, ModuleId, IsActive FROM dbo.TenantModule WHERE TenantId = @TenantId AND ModuleId = @ModuleId";
            return await connection.QuerySingleOrDefaultAsync<TenantModule>(
                new CommandDefinition(sql, new { TenantId = tenantId, ModuleId = moduleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsModuleActiveAsync(Guid tenantId, string moduleKey, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.TenantModule tm
                    INNER JOIN dbo.Module m ON m.Id = tm.ModuleId
                    WHERE tm.TenantId = @TenantId AND m.[Key] = @ModuleKey AND tm.IsActive = 1
                ) THEN 1 ELSE 0 END AS BIT)";
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { TenantId = tenantId, ModuleKey = moduleKey }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<string>> GetActiveModuleKeysAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT m.[Key] FROM dbo.TenantModule tm
                INNER JOIN dbo.Module m ON m.Id = tm.ModuleId
                WHERE tm.TenantId = @TenantId AND tm.IsActive = 1
                ORDER BY m.Name";
            var keys = await connection.QueryAsync<string>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return keys.ToList();
        }
    }

    public async Task<IReadOnlyList<TenantModule>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, TenantId, ModuleId, IsActive FROM dbo.TenantModule WHERE TenantId = @TenantId";
            var list = await connection.QueryAsync<TenantModule>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task SetActiveAsync(Guid tenantId, Guid moduleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.TenantModule SET IsActive = @IsActive WHERE TenantId = @TenantId AND ModuleId = @ModuleId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, ModuleId = moduleId, IsActive = isActive }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task EnsureTenantModuleAsync(Guid tenantId, Guid moduleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string checkSql = "SELECT Id FROM dbo.TenantModule WHERE TenantId = @TenantId AND ModuleId = @ModuleId";
            var existingId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(checkSql, new { TenantId = tenantId, ModuleId = moduleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (existingId.HasValue)
            {
                const string updateSql = "UPDATE dbo.TenantModule SET IsActive = @IsActive WHERE TenantId = @TenantId AND ModuleId = @ModuleId";
                await connection.ExecuteAsync(new CommandDefinition(updateSql, new { TenantId = tenantId, ModuleId = moduleId, IsActive = isActive }, cancellationToken: cancellationToken)).ConfigureAwait(false);
                return;
            }
            const string insertSql = "INSERT INTO dbo.TenantModule (Id, TenantId, ModuleId, IsActive) VALUES (@Id, @TenantId, @ModuleId, @IsActive)";
            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = moduleId,
                IsActive = isActive
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

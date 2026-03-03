using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TenantUsageRepository : ITenantUsageRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TenantUsageRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantUsage?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [TenantId], [ProjectsCount], [UsersCount], [StorageUsedMB], [TimeLogsCount], [UpdatedAt] FROM dbo.[TenantUsage] WHERE [TenantId] = @TenantId";
            return await connection.QuerySingleOrDefaultAsync<TenantUsage>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task EnsureExistsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.[TenantUsage] WHERE [TenantId] = @TenantId)
    INSERT INTO dbo.[TenantUsage] ([TenantId], [ProjectsCount], [UsersCount], [StorageUsedMB], [TimeLogsCount], [UpdatedAt])
    VALUES (@TenantId, 0, 0, 0, 0, GETUTCDATE());";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task IncrementProjectsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await UpdateDeltaAsync(tenantId, "ProjectsCount", 1, cancellationToken).ConfigureAwait(false);
    }

    public async Task DecrementProjectsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.[TenantUsage] SET [ProjectsCount] = CASE WHEN [ProjectsCount] - 1 < 0 THEN 0 ELSE [ProjectsCount] - 1 END, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
        await ExecuteUpdateAsync(tenantId, sql, cancellationToken).ConfigureAwait(false);
    }

    public async Task IncrementUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await UpdateDeltaAsync(tenantId, "UsersCount", 1, cancellationToken).ConfigureAwait(false);
    }

    public async Task DecrementUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.[TenantUsage] SET [UsersCount] = CASE WHEN [UsersCount] - 1 < 0 THEN 0 ELSE [UsersCount] - 1 END, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
        await ExecuteUpdateAsync(tenantId, sql, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddStorageMbAsync(Guid tenantId, decimal mb, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.[TenantUsage] SET [StorageUsedMB] = [StorageUsedMB] + @Mb, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, Mb = mb }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RemoveStorageMbAsync(Guid tenantId, decimal mb, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.[TenantUsage] SET [StorageUsedMB] = CASE WHEN [StorageUsedMB] - @Mb < 0 THEN 0 ELSE [StorageUsedMB] - @Mb END, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, Mb = mb }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task IncrementTimeLogsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await UpdateDeltaAsync(tenantId, "TimeLogsCount", 1, cancellationToken).ConfigureAwait(false);
    }

    public async Task DecrementTimeLogsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.[TenantUsage] SET [TimeLogsCount] = CASE WHEN [TimeLogsCount] - 1 < 0 THEN 0 ELSE [TimeLogsCount] - 1 END, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
        await ExecuteUpdateAsync(tenantId, sql, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantUsage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT [TenantId], [ProjectsCount], [UsersCount], [StorageUsedMB], [TimeLogsCount], [UpdatedAt] FROM dbo.[TenantUsage]";
            var list = await connection.QueryAsync<TenantUsage>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    private async Task UpdateDeltaAsync(Guid tenantId, string column, int delta, CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var sql = $"UPDATE dbo.[TenantUsage] SET [{column}] = [{column}] + @Delta, [UpdatedAt] = GETUTCDATE() WHERE [TenantId] = @TenantId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, Delta = delta }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    private async Task ExecuteUpdateAsync(Guid tenantId, string sql, CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

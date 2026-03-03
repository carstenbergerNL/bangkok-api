using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ModuleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, Name, [Key], Description FROM dbo.Module WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Module>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, Name, [Key], Description FROM dbo.Module WHERE [Key] = @Key";
            return await connection.QuerySingleOrDefaultAsync<Module>(
                new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, Name, [Key], Description FROM dbo.Module ORDER BY Name";
            var list = await connection.QueryAsync<Module>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }
}

using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PermissionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description
                FROM dbo.Permission
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Permission>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description
                FROM dbo.Permission
                WHERE Name = @Name";
            return await connection.QuerySingleOrDefaultAsync<Permission>(new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description
                FROM dbo.Permission
                ORDER BY Name";
            var items = await connection.QueryAsync<Permission>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<Guid> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Permission (Id, Name, Description)
                VALUES (@Id, @Name, @Description)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                permission.Id,
                permission.Name,
                permission.Description
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return permission.Id;
        }
    }

    public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.Permission
                SET Name = @Name, Description = @Description
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                permission.Id,
                permission.Name,
                permission.Description
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.RolePermission WHERE PermissionId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.Permission WHERE Id = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                transaction.Commit();
            }
        }
    }
}

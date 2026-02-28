using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RoleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description, CreatedAt
                FROM dbo.Role
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Role>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description, CreatedAt
                FROM dbo.Role
                WHERE Name = @Name";
            return await connection.QuerySingleOrDefaultAsync<Role>(new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, Name, Description, CreatedAt
                FROM dbo.Role
                ORDER BY Name";
            var items = await connection.QueryAsync<Role>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<Guid> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Role (Id, Name, Description, CreatedAt)
                VALUES (@Id, @Name, @Description, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                role.Id,
                role.Name,
                role.Description,
                role.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return role.Id;
        }
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.Role
                SET Name = @Name, Description = @Description
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                role.Id,
                role.Name,
                role.Description
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
                    new CommandDefinition("DELETE FROM dbo.RolePermission WHERE RoleId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.UserRole WHERE RoleId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.Role WHERE Id = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                transaction.Commit();
            }
        }
    }
}

using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public LabelRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Label?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, Name, Color, ProjectId, CreatedAt FROM dbo.Label WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Label>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Label>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, Name, Color, ProjectId, CreatedAt FROM dbo.Label WHERE ProjectId = @ProjectId ORDER BY Name";
            var items = await connection.QueryAsync<Label>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<Guid> AddAsync(Label label, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "INSERT INTO dbo.Label (Id, Name, Color, ProjectId, CreatedAt) VALUES (@Id, @Name, @Color, @ProjectId, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { label.Id, label.Name, label.Color, label.ProjectId, label.CreatedAt }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return label.Id;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskLabel WHERE LabelId = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.Label WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

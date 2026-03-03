using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectCustomFieldRepository : IProjectCustomFieldRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectCustomFieldRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProjectCustomField?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, ProjectId, Name, FieldType, Options, CreatedAt FROM dbo.ProjectCustomField WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectCustomField>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ProjectCustomField>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, ProjectId, Name, FieldType, Options, CreatedAt FROM dbo.ProjectCustomField WHERE ProjectId = @ProjectId ORDER BY CreatedAt";
            var list = await connection.QueryAsync<ProjectCustomField>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(ProjectCustomField field, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.ProjectCustomField (Id, ProjectId, Name, FieldType, Options, CreatedAt)
                VALUES (@Id, @ProjectId, @Name, @FieldType, @Options, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                field.Id,
                field.ProjectId,
                field.Name,
                field.FieldType,
                field.Options,
                field.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return field.Id;
        }
    }

    public async Task UpdateAsync(ProjectCustomField field, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.ProjectCustomField SET Name = @Name, FieldType = @FieldType, Options = @Options WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                field.Id,
                field.Name,
                field.FieldType,
                field.Options
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM dbo.ProjectCustomField WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

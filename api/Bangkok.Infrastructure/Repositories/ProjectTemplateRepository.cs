using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectTemplateRepository : IProjectTemplateRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectTemplateRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"SELECT Id, Name, Description, CreatedAt FROM dbo.ProjectTemplate WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectTemplate>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ProjectTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"SELECT Id, Name, Description, CreatedAt FROM dbo.ProjectTemplate ORDER BY CreatedAt DESC";
            var list = await connection.QueryAsync<ProjectTemplate>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(ProjectTemplate template, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.ProjectTemplate (Id, Name, Description, CreatedAt)
                VALUES (@Id, @Name, @Description, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                template.Id,
                template.Name,
                template.Description,
                template.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return template.Id;
        }
    }

    public async Task UpdateAsync(ProjectTemplate template, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.ProjectTemplate SET Name = @Name, Description = @Description WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                template.Id,
                template.Name,
                template.Description
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
                new CommandDefinition("DELETE FROM dbo.ProjectTemplate WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

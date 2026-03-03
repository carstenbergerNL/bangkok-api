using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectTemplateTaskRepository : IProjectTemplateTaskRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectTemplateTaskRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProjectTemplateTask>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TemplateId, Title, Description, DefaultStatus, DefaultPriority
                FROM dbo.ProjectTemplateTask WHERE TemplateId = @TemplateId ORDER BY Id";
            var list = await connection.QueryAsync<ProjectTemplateTask>(
                new CommandDefinition(sql, new { TemplateId = templateId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task CreateAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.ProjectTemplateTask (Id, TemplateId, Title, Description, DefaultStatus, DefaultPriority)
                VALUES (@Id, @TemplateId, @Title, @Description, @DefaultStatus, @DefaultPriority)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                task.Id,
                task.TemplateId,
                task.Title,
                task.Description,
                task.DefaultStatus,
                task.DefaultPriority
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM dbo.ProjectTemplateTask WHERE TemplateId = @TemplateId", new { TemplateId = templateId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

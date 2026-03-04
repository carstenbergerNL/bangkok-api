using Bangkok.Domain;
using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectAutomationRuleRepository : IProjectAutomationRuleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectAutomationRuleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProjectAutomationRule>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, ProjectId, [Trigger], [Action], TargetUserId, TargetValue FROM dbo.ProjectAutomationRule WHERE ProjectId = @ProjectId ORDER BY [Trigger], [Action]";
            var list = await connection.QueryAsync<ProjectAutomationRule>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<ProjectAutomationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT Id, ProjectId, [Trigger], [Action], TargetUserId, TargetValue FROM dbo.ProjectAutomationRule WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectAutomationRule>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(ProjectAutomationRule rule, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.ProjectAutomationRule (Id, ProjectId, [Trigger], [Action], TargetUserId, TargetValue)
                VALUES (@Id, @ProjectId, @Trigger, @Action, @TargetUserId, @TargetValue)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                rule.Id,
                rule.ProjectId,
                rule.Trigger,
                rule.Action,
                rule.TargetUserId,
                rule.TargetValue
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return rule.Id;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.ProjectAutomationRule WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

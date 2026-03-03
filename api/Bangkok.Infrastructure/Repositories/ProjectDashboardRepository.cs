using System.Data;
using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectDashboardRepository : IProjectDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectDashboardRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProjectDashboardResponse> GetDashboardAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var utcNow = DateTime.UtcNow;

            const string totalsSql = @"
                SELECT
                    COUNT(*) AS TotalTasks,
                    ISNULL(SUM(CASE WHEN Status = 'Done' THEN 1 ELSE 0 END), 0) AS CompletedTasks,
                    ISNULL(SUM(CASE WHEN DueDate IS NOT NULL AND DueDate < @UtcNow AND Status <> 'Done' THEN 1 ELSE 0 END), 0) AS OverdueTasks,
                    ISNULL(SUM(t.EstimatedHours), 0) AS TotalEstimatedHours
                FROM dbo.Task t
                WHERE t.ProjectId = @ProjectId";
            var totals = await connection.QuerySingleAsync<(int TotalTasks, int CompletedTasks, int OverdueTasks, decimal TotalEstimatedHours)>(
                new CommandDefinition(totalsSql, new { ProjectId = projectId, UtcNow = utcNow }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string loggedSql = @"
                SELECT ISNULL(SUM(l.Hours), 0)
                FROM dbo.TaskTimeLog l
                INNER JOIN dbo.Task t ON l.TaskId = t.Id
                WHERE t.ProjectId = @ProjectId";
            var totalLoggedHours = await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(loggedSql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string overBudgetSql = @"
                SELECT COUNT(*)
                FROM dbo.Task t
                WHERE t.ProjectId = @ProjectId
                  AND t.EstimatedHours IS NOT NULL
                  AND (SELECT ISNULL(SUM(l.Hours), 0) FROM dbo.TaskTimeLog l WHERE l.TaskId = t.Id) > t.EstimatedHours";
            var overBudgetCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(overBudgetSql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string statusSql = @"
                SELECT Status, COUNT(*) AS Count
                FROM dbo.Task
                WHERE ProjectId = @ProjectId
                GROUP BY Status";
            var statusRows = await connection.QueryAsync<(string Status, int Count)>(
                new CommandDefinition(statusSql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string memberSql = @"
                SELECT t.AssignedToUserId AS UserId, u.DisplayName AS UserDisplayName, COUNT(*) AS Count
                FROM dbo.Task t
                INNER JOIN dbo.[User] u ON t.AssignedToUserId = u.Id
                WHERE t.ProjectId = @ProjectId AND t.AssignedToUserId IS NOT NULL
                GROUP BY t.AssignedToUserId, u.DisplayName";
            var memberRows = await connection.QueryAsync<(Guid UserId, string? UserDisplayName, int Count)>(
                new CommandDefinition(memberSql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

            return new ProjectDashboardResponse
            {
                TotalTasks = totals.TotalTasks,
                CompletedTasks = totals.CompletedTasks,
                OverdueTasks = totals.OverdueTasks,
                TotalEstimatedHours = totals.TotalEstimatedHours,
                TotalLoggedHours = totalLoggedHours,
                OverBudgetTaskCount = overBudgetCount,
                TasksPerStatus = statusRows.Select(x => new TasksPerStatusItem { Status = x.Status, Count = x.Count }).ToList(),
                TasksPerMember = memberRows.Select(x => new TasksPerMemberItem { UserId = x.UserId, UserDisplayName = x.UserDisplayName, Count = x.Count }).ToList()
            };
        }
    }
}

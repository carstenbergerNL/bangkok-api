using System.Data;
using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, CreatedByUserId, CreatedAt, UpdatedAt, EstimatedHours,
                    ISNULL(IsRecurring, 0) AS IsRecurring, RecurrencePattern, RecurrenceInterval, RecurrenceEndDate, RecurrenceSourceTaskId
                FROM dbo.Task
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectTask>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ProjectTask>> GetByProjectIdAsync(Guid projectId, TaskFilterRequest? filter, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var conditions = new List<string> { "t.ProjectId = @ProjectId" };
            var param = new DynamicParameters();
            param.Add("ProjectId", projectId);

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    conditions.Add("t.Status = @Status");
                    param.Add("Status", filter.Status.Trim());
                }
                if (!string.IsNullOrWhiteSpace(filter.Priority))
                {
                    conditions.Add("t.Priority = @Priority");
                    param.Add("Priority", filter.Priority.Trim());
                }
                if (filter.AssignedToUserId.HasValue && filter.AssignedToUserId.Value != Guid.Empty)
                {
                    conditions.Add("t.AssignedToUserId = @AssignedToUserId");
                    param.Add("AssignedToUserId", filter.AssignedToUserId.Value);
                }
                if (filter.LabelId.HasValue && filter.LabelId.Value != Guid.Empty)
                {
                    conditions.Add("EXISTS (SELECT 1 FROM dbo.TaskLabel tl WHERE tl.TaskId = t.Id AND tl.LabelId = @LabelId)");
                    param.Add("LabelId", filter.LabelId.Value);
                }
                if (filter.DueBefore.HasValue)
                {
                    conditions.Add("t.DueDate IS NOT NULL AND t.DueDate <= @DueBefore");
                    param.Add("DueBefore", filter.DueBefore.Value);
                }
                if (filter.DueAfter.HasValue)
                {
                    conditions.Add("t.DueDate IS NOT NULL AND t.DueDate >= @DueAfter");
                    param.Add("DueAfter", filter.DueAfter.Value);
                }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var searchTerm = "%" + filter.Search.Trim().Replace("%", "[%]").Replace("[", "[[]") + "%";
                    conditions.Add("(t.Title LIKE @SearchTerm OR (t.Description IS NOT NULL AND t.Description LIKE @SearchTerm))");
                    param.Add("SearchTerm", searchTerm);
                }
            }

            var whereClause = string.Join(" AND ", conditions);
            var sql = $@"
                SELECT t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.AssignedToUserId, t.DueDate, t.CreatedByUserId, t.CreatedAt, t.UpdatedAt, t.EstimatedHours,
                    ISNULL(t.IsRecurring, 0) AS IsRecurring, t.RecurrencePattern, t.RecurrenceInterval, t.RecurrenceEndDate, t.RecurrenceSourceTaskId
                FROM dbo.Task t
                WHERE {whereClause}
                ORDER BY t.CreatedAt DESC";
            var items = await connection.QueryAsync<ProjectTask>(
                new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<Guid> CreateAsync(ProjectTask task, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Task (Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, CreatedByUserId, CreatedAt, EstimatedHours, IsRecurring, RecurrencePattern, RecurrenceInterval, RecurrenceEndDate, RecurrenceSourceTaskId)
                VALUES (@Id, @ProjectId, @Title, @Description, @Status, @Priority, @AssignedToUserId, @DueDate, @CreatedByUserId, @CreatedAt, @EstimatedHours, @IsRecurring, @RecurrencePattern, @RecurrenceInterval, @RecurrenceEndDate, @RecurrenceSourceTaskId)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.AssignedToUserId,
                task.DueDate,
                task.CreatedByUserId,
                task.CreatedAt,
                task.EstimatedHours,
                task.IsRecurring,
                task.RecurrencePattern,
                task.RecurrenceInterval,
                task.RecurrenceEndDate,
                task.RecurrenceSourceTaskId
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return task.Id;
        }
    }

    public async Task UpdateAsync(ProjectTask task, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.Task
                SET Title = @Title, Description = @Description, Status = @Status, Priority = @Priority,
                    AssignedToUserId = @AssignedToUserId, DueDate = @DueDate, UpdatedAt = @UpdatedAt, EstimatedHours = @EstimatedHours,
                    IsRecurring = @IsRecurring, RecurrencePattern = @RecurrencePattern, RecurrenceInterval = @RecurrenceInterval, RecurrenceEndDate = @RecurrenceEndDate, RecurrenceSourceTaskId = @RecurrenceSourceTaskId
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                task.Id,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.AssignedToUserId,
                task.DueDate,
                task.UpdatedAt,
                task.EstimatedHours,
                task.IsRecurring,
                task.RecurrencePattern,
                task.RecurrenceInterval,
                task.RecurrenceEndDate,
                task.RecurrenceSourceTaskId
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
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskTimeLog WHERE TaskId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskLabel WHERE TaskId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskActivity WHERE TaskId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.TaskComment WHERE TaskId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.Task WHERE Id = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                transaction.Commit();
            }
        }
    }
}

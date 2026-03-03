using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskActivityRepository : ITaskActivityRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskActivityRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TaskActivity>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, UserId, Action, OldValue, NewValue, CreatedAt
                FROM dbo.TaskActivity
                WHERE TaskId = @TaskId
                ORDER BY CreatedAt DESC";
            var list = await connection.QueryAsync<TaskActivity>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(TaskActivity activity, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.TaskActivity (Id, TaskId, UserId, Action, OldValue, NewValue, CreatedAt)
                VALUES (@Id, @TaskId, @UserId, @Action, @OldValue, @NewValue, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                activity.Id,
                activity.TaskId,
                activity.UserId,
                activity.Action,
                activity.OldValue,
                activity.NewValue,
                activity.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return activity.Id;
        }
    }
}

using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskTimeLogRepository : ITaskTimeLogRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskTimeLogRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TaskTimeLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, UserId, Hours, Description, CreatedAt
                FROM dbo.TaskTimeLog WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<TaskTimeLog>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TaskTimeLog>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, UserId, Hours, Description, CreatedAt
                FROM dbo.TaskTimeLog WHERE TaskId = @TaskId ORDER BY CreatedAt DESC";
            var list = await connection.QueryAsync<TaskTimeLog>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<decimal> GetTotalLoggedHoursByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT ISNULL(SUM(Hours), 0) FROM dbo.TaskTimeLog WHERE TaskId = @TaskId";
            return await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(TaskTimeLog log, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.TaskTimeLog (Id, TaskId, UserId, Hours, Description, CreatedAt)
                VALUES (@Id, @TaskId, @UserId, @Hours, @Description, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                log.Id,
                log.TaskId,
                log.UserId,
                log.Hours,
                log.Description,
                log.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return log.Id;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TaskTimeLog WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

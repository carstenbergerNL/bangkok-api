using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskCommentRepository : ITaskCommentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskCommentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TaskComment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, UserId, Content, CreatedAt, UpdatedAt
                FROM dbo.TaskComment
                WHERE TaskId = @TaskId
                ORDER BY CreatedAt ASC";
            var list = await connection.QueryAsync<TaskComment>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<TaskComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, UserId, Content, CreatedAt, UpdatedAt
                FROM dbo.TaskComment
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<TaskComment>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(TaskComment comment, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.TaskComment (Id, TaskId, UserId, Content, CreatedAt)
                VALUES (@Id, @TaskId, @UserId, @Content, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                comment.Id,
                comment.TaskId,
                comment.UserId,
                comment.Content,
                comment.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return comment.Id;
        }
    }

    public async Task UpdateAsync(TaskComment comment, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.TaskComment
                SET Content = @Content, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                comment.Id,
                comment.Content,
                comment.UpdatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TaskComment WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

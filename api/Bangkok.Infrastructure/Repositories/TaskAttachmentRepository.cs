using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TaskAttachmentRepository : ITaskAttachmentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TaskAttachmentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, FileName, FilePath, FileSize, ContentType, UploadedByUserId, CreatedAt
                FROM dbo.TaskAttachment WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<TaskAttachment>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TaskId, FileName, FilePath, FileSize, ContentType, UploadedByUserId, CreatedAt
                FROM dbo.TaskAttachment WHERE TaskId = @TaskId ORDER BY CreatedAt DESC";
            var list = await connection.QueryAsync<TaskAttachment>(
                new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<Guid> CreateAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.TaskAttachment (Id, TaskId, FileName, FilePath, FileSize, ContentType, UploadedByUserId, CreatedAt)
                VALUES (@Id, @TaskId, @FileName, @FilePath, @FileSize, @ContentType, @UploadedByUserId, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                attachment.Id,
                attachment.TaskId,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileSize,
                attachment.ContentType,
                attachment.UploadedByUserId,
                attachment.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return attachment.Id;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            await connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM dbo.TaskAttachment WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

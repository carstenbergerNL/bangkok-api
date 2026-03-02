using System.Data;
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
                SELECT Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, CreatedByUserId, CreatedAt, UpdatedAt
                FROM dbo.Task
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectTask>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ProjectTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, CreatedByUserId, CreatedAt, UpdatedAt
                FROM dbo.Task
                WHERE ProjectId = @ProjectId
                ORDER BY CreatedAt DESC";
            var items = await connection.QueryAsync<ProjectTask>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
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
                INSERT INTO dbo.Task (Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, DueDate, CreatedByUserId, CreatedAt)
                VALUES (@Id, @ProjectId, @Title, @Description, @Status, @Priority, @AssignedToUserId, @DueDate, @CreatedByUserId, @CreatedAt)";
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
                task.CreatedAt
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
                    AssignedToUserId = @AssignedToUserId, DueDate = @DueDate, UpdatedAt = @UpdatedAt
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
                task.UpdatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.Task WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

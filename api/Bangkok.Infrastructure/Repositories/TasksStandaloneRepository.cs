using System.Data;
using Bangkok.Application.Dto.TasksStandalone;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class TasksStandaloneRepository : ITasksStandaloneRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TasksStandaloneRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TasksStandalone?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"SELECT Id, TenantId, Title, Description, Status, Priority, AssignedToUserId, CreatedByUserId, DueDate, CreatedAt, UpdatedAt
FROM dbo.TasksStandalone WHERE Id = @Id AND TenantId = @TenantId";
            return await connection.QuerySingleOrDefaultAsync<TasksStandalone>(
                new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TasksStandalone>> GetListAsync(Guid tenantId, TasksStandaloneFilterRequest? filter, Guid? assignedToUserIdOnly, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var sql = @"SELECT Id, TenantId, Title, Description, Status, Priority, AssignedToUserId, CreatedByUserId, DueDate, CreatedAt, UpdatedAt
FROM dbo.TasksStandalone WHERE TenantId = @TenantId";
            var parameters = new DynamicParameters();
            parameters.Add("TenantId", tenantId);

            if (assignedToUserIdOnly.HasValue)
            {
                sql += " AND AssignedToUserId = @AssignedToUserId";
                parameters.Add("AssignedToUserId", assignedToUserIdOnly.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter?.Status))
            {
                sql += " AND Status = @Status";
                parameters.Add("Status", filter.Status.Trim());
            }
            if (filter?.AssignedToUserId.HasValue == true)
            {
                sql += " AND AssignedToUserId = @AssignedToUserIdFilter";
                parameters.Add("AssignedToUserIdFilter", filter.AssignedToUserId!.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter?.Priority))
            {
                sql += " AND Priority = @Priority";
                parameters.Add("Priority", filter.Priority.Trim());
            }
            if (filter?.DueBefore.HasValue == true)
            {
                sql += " AND DueDate IS NOT NULL AND DueDate <= @DueBefore";
                parameters.Add("DueBefore", filter.DueBefore.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter?.Search))
            {
                sql += " AND (Title LIKE @Search OR Description LIKE @Search)";
                parameters.Add("Search", "%" + filter.Search.Trim() + "%");
            }

            sql += " ORDER BY CASE WHEN DueDate IS NULL THEN 1 ELSE 0 END, DueDate, CreatedAt DESC";

            var items = await connection.QueryAsync<TasksStandalone>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<int> GetCountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT COUNT(*) FROM dbo.TasksStandalone WHERE TenantId = @TenantId";
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(TasksStandalone entity, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"INSERT INTO dbo.TasksStandalone (Id, TenantId, Title, Description, Status, Priority, AssignedToUserId, CreatedByUserId, DueDate, CreatedAt, UpdatedAt)
VALUES (@Id, @TenantId, @Title, @Description, @Status, @Priority, @AssignedToUserId, @CreatedByUserId, @DueDate, @CreatedAt, @UpdatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                entity.Id,
                entity.TenantId,
                entity.Title,
                entity.Description,
                entity.Status,
                entity.Priority,
                entity.AssignedToUserId,
                entity.CreatedByUserId,
                entity.DueDate,
                entity.CreatedAt,
                entity.UpdatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return entity.Id;
        }
    }

    public async Task UpdateAsync(TasksStandalone entity, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"UPDATE dbo.TasksStandalone SET Title = @Title, Description = @Description, Status = @Status, Priority = @Priority,
AssignedToUserId = @AssignedToUserId, DueDate = @DueDate, UpdatedAt = @UpdatedAt WHERE Id = @Id AND TenantId = @TenantId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                entity.Id,
                entity.TenantId,
                entity.Title,
                entity.Description,
                entity.Status,
                entity.Priority,
                entity.AssignedToUserId,
                entity.DueDate,
                entity.UpdatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.TasksStandalone WHERE Id = @Id AND TenantId = @TenantId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

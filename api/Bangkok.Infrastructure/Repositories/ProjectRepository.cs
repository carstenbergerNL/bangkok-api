using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt
                FROM dbo.Project
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Project>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(Guid? tenantId, string? status = null, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var hasTenant = tenantId.HasValue;
            var hasStatus = !string.IsNullOrWhiteSpace(status);
            string sql;
            object? param;
            if (hasTenant && hasStatus)
            {
                sql = "SELECT Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt FROM dbo.Project WHERE TenantId = @TenantId AND Status = @Status ORDER BY CreatedAt DESC";
                param = new { TenantId = tenantId!.Value, Status = status!.Trim() };
            }
            else if (hasTenant)
            {
                sql = "SELECT Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt FROM dbo.Project WHERE TenantId = @TenantId ORDER BY CreatedAt DESC";
                param = new { TenantId = tenantId!.Value };
            }
            else if (hasStatus)
            {
                sql = "SELECT Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt FROM dbo.Project WHERE Status = @Status ORDER BY CreatedAt DESC";
                param = new { Status = status!.Trim() };
            }
            else
            {
                sql = "SELECT Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt, UpdatedAt FROM dbo.Project ORDER BY CreatedAt DESC";
                param = null;
            }
            var items = param != null
                ? await connection.QueryAsync<Project>(new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false)
                : await connection.QueryAsync<Project>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return items.ToList();
        }
    }

    public async Task<Guid> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Project (Id, TenantId, Name, Description, Status, CreatedByUserId, CreatedAt)
                VALUES (@Id, @TenantId, @Name, @Description, @Status, @CreatedByUserId, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                project.Id,
                project.TenantId,
                project.Name,
                project.Description,
                project.Status,
                project.CreatedByUserId,
                project.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return project.Id;
        }
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.Project
                SET Name = @Name, Description = @Description, Status = @Status, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                project.Id,
                project.Name,
                project.Description,
                project.Status,
                project.UpdatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.Project WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<int> GetTaskCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT COUNT(1) FROM dbo.Task WHERE ProjectId = @ProjectId";
            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

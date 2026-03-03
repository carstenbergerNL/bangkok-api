using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProjectMemberRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, ProjectId, UserId, Role, CreatedAt
                FROM dbo.ProjectMember
                WHERE ProjectId = @ProjectId AND UserId = @UserId";
            return await connection.QuerySingleOrDefaultAsync<ProjectMember>(
                new CommandDefinition(sql, new { ProjectId = projectId, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetProjectIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT ProjectId FROM dbo.ProjectMember WHERE UserId = @UserId";
            var list = await connection.QueryAsync<Guid>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<IReadOnlyList<ProjectMember>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, ProjectId, UserId, Role, CreatedAt
                FROM dbo.ProjectMember
                WHERE ProjectId = @ProjectId
                ORDER BY Role ASC, CreatedAt ASC";
            var list = await connection.QueryAsync<ProjectMember>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<ProjectMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, ProjectId, UserId, Role, CreatedAt
                FROM dbo.ProjectMember
                WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<ProjectMember>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<int> CountOwnersAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT COUNT(1) FROM dbo.ProjectMember WHERE ProjectId = @ProjectId AND Role = N'Owner'";
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.ProjectMember (Id, ProjectId, UserId, Role, CreatedAt)
                VALUES (@Id, @ProjectId, @UserId, @Role, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                member.Id,
                member.ProjectId,
                member.UserId,
                member.Role,
                member.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task UpdateAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.ProjectMember SET Role = @Role WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { member.Id, member.Role }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.ProjectMember WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.ProjectMember WHERE ProjectId = @ProjectId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

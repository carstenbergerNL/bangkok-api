using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public NotificationRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, UserId, Type, Title, Message, ReferenceId, IsRead, CreatedAt
                FROM dbo.Notification WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Notification>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT TOP (@Limit) Id, UserId, Type, Title, Message, ReferenceId, IsRead, CreatedAt
                FROM dbo.Notification WHERE UserId = @UserId
                ORDER BY CreatedAt DESC";
            var list = await connection.QueryAsync<Notification>(
                new CommandDefinition(sql, new { UserId = userId, Limit = limit }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "SELECT COUNT(1) FROM dbo.Notification WHERE UserId = @UserId AND IsRead = 0";
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Notification (Id, UserId, Type, Title, Message, ReferenceId, IsRead, CreatedAt)
                VALUES (@Id, @UserId, @Type, @Title, @Message, @ReferenceId, @IsRead, @CreatedAt)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                notification.Id,
                notification.UserId,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.ReferenceId,
                notification.IsRead,
                notification.CreatedAt
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return notification.Id;
        }
    }

    public async Task MarkReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.Notification SET IsRead = 1 WHERE Id = @Id AND UserId = @UserId";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "UPDATE dbo.Notification SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

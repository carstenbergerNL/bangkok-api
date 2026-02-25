using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
            FROM dbo.[User]
            WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
            FROM dbo.[User]
            WHERE Email = @Email";
            return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<User?> GetByRecoverStringAsync(string recoverString, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
            FROM dbo.[User]
            WHERE RecoverString = @RecoverString";
            return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new { RecoverString = recoverString }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            INSERT INTO dbo.[User] (Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, @PasswordSalt, @Role, @IsActive, @CreatedAtUtc)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                user.Id,
                user.Email,
                user.DisplayName,
                user.PasswordHash,
                user.PasswordSalt,
                user.Role,
                user.IsActive,
                user.CreatedAtUtc
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return user.Id;
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.[User]
            SET Email = @Email, DisplayName = @DisplayName, PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, Role = @Role,
                IsActive = @IsActive, UpdatedAtUtc = @UpdatedAtUtc, RecoverString = @RecoverString, RecoverStringExpiry = @RecoverStringExpiry
            WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                user.Id,
                user.Email,
                user.DisplayName,
                user.PasswordHash,
                user.PasswordSalt,
                user.Role,
                user.IsActive,
                user.UpdatedAtUtc,
                user.RecoverString,
                user.RecoverStringExpiry
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
        var offset = (pageNumber - 1) * pageSize;

        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string countSql = "SELECT COUNT(*) FROM dbo.[User]";
            var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string sql = @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
            FROM dbo.[User]
            ORDER BY CreatedAtUtc
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = (await connection.QueryAsync<User>(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            return (items, totalCount);
        }
    }
}

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
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
            FROM dbo.[User]
            WHERE Id = @Id AND IsDeleted = 0";
            return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<User?> GetByIdIncludeDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
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
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
            FROM dbo.[User]
            WHERE Email = @Email AND IsDeleted = 0";
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
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
            FROM dbo.[User]
            WHERE RecoverString = @RecoverString AND IsDeleted = 0";
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
            INSERT INTO dbo.[User] (Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, IsDeleted)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, @PasswordSalt, @Role, @IsActive, @CreatedAtUtc, 0)";
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
                IsActive = @IsActive, UpdatedAtUtc = @UpdatedAtUtc, RecoverString = @RecoverString, RecoverStringExpiry = @RecoverStringExpiry,
                IsDeleted = @IsDeleted, DeletedAt = @DeletedAt, FailedLoginAttempts = @FailedLoginAttempts, LockoutEnd = @LockoutEnd
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
                user.RecoverStringExpiry,
                user.IsDeleted,
                user.DeletedAt,
                user.FailedLoginAttempts,
                user.LockoutEnd
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        var maxPageSize = includeDeleted ? 500 : 100;
        if (pageSize < 1 || pageSize > maxPageSize) pageSize = includeDeleted ? 500 : 10;
        var offset = (pageNumber - 1) * pageSize;

        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            var countSql = includeDeleted ? "SELECT COUNT(*) FROM dbo.[User]" : "SELECT COUNT(*) FROM dbo.[User] WHERE IsDeleted = 0";
            var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, cancellationToken: cancellationToken)).ConfigureAwait(false);

            var sql = includeDeleted
                ? @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
            FROM dbo.[User]
            ORDER BY IsDeleted ASC, DeletedAt DESC, CreatedAtUtc
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                : @"
            SELECT Id, Email, DisplayName, PasswordHash, PasswordSalt, Role, IsActive, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry, IsDeleted, DeletedAt, FailedLoginAttempts, LockoutEnd
            FROM dbo.[User]
            WHERE IsDeleted = 0
            ORDER BY CreatedAtUtc
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = (await connection.QueryAsync<User>(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            return (items, totalCount);
        }
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.[User]
            SET IsDeleted = 1, DeletedAt = GETUTCDATE()
            WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.[User]
            SET IsDeleted = 0, DeletedAt = NULL
            WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.RefreshToken WHERE UserId = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    new CommandDefinition("DELETE FROM dbo.[User] WHERE Id = @Id", new { Id = id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                transaction.Commit();
            }
        }
    }

    public async Task ClearLockoutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.[User]
            SET FailedLoginAttempts = 0, LockoutEnd = NULL, UpdatedAtUtc = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task SetLockoutAsync(Guid id, DateTime lockoutEnd, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.[User]
            SET FailedLoginAttempts = 10, LockoutEnd = @LockoutEnd, UpdatedAtUtc = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, LockoutEnd = lockoutEnd }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

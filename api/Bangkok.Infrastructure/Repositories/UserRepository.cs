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
            SELECT Id, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
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
            SELECT Id, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
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
            SELECT Id, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, UpdatedAtUtc, RecoverString, RecoverStringExpiry
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
            INSERT INTO dbo.[User] (Id, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc)
            VALUES (@Id, @Email, @PasswordHash, @PasswordSalt, @Role, @CreatedAtUtc)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.PasswordSalt,
                user.Role,
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
            SET Email = @Email, PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, Role = @Role,
                UpdatedAtUtc = @UpdatedAtUtc, RecoverString = @RecoverString, RecoverStringExpiry = @RecoverStringExpiry
            WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.PasswordSalt,
                user.Role,
                user.UpdatedAtUtc,
                user.RecoverString,
                user.RecoverStringExpiry
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

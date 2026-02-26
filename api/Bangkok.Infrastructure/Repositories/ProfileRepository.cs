using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProfileRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                SELECT Id, UserId, FirstName, MiddleName, LastName, DateOfBirth, PhoneNumber, AvatarBase64, CreatedAtUtc, UpdatedAtUtc
                FROM dbo.Profile
                WHERE UserId = @UserId";
            return await connection.QuerySingleOrDefaultAsync<Profile>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                INSERT INTO dbo.Profile (Id, UserId, FirstName, MiddleName, LastName, DateOfBirth, PhoneNumber, AvatarBase64, CreatedAtUtc)
                VALUES (@Id, @UserId, @FirstName, @MiddleName, @LastName, @DateOfBirth, @PhoneNumber, @AvatarBase64, @CreatedAtUtc)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                profile.Id,
                profile.UserId,
                profile.FirstName,
                profile.MiddleName,
                profile.LastName,
                profile.DateOfBirth,
                profile.PhoneNumber,
                profile.AvatarBase64,
                profile.CreatedAtUtc
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return profile.Id;
        }
    }

    public async Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
                UPDATE dbo.Profile
                SET FirstName = @FirstName, MiddleName = @MiddleName, LastName = @LastName, DateOfBirth = @DateOfBirth,
                    PhoneNumber = @PhoneNumber, AvatarBase64 = @AvatarBase64, UpdatedAtUtc = @UpdatedAtUtc
                WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                profile.Id,
                profile.FirstName,
                profile.MiddleName,
                profile.LastName,
                profile.DateOfBirth,
                profile.PhoneNumber,
                profile.AvatarBase64,
                profile.UpdatedAtUtc
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = "DELETE FROM dbo.Profile WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

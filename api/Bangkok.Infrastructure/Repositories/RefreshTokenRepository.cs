using System.Data;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Bangkok.Infrastructure.Data;
using Dapper;

namespace Bangkok.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RefreshTokenRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            SELECT Id, UserId, Token, ExpiresAtUtc, CreatedAtUtc, RevokedReason, RevokedAtUtc
            FROM dbo.RefreshToken
            WHERE Token = @Token AND RevokedAtUtc IS NULL";
            return await connection.QuerySingleOrDefaultAsync<RefreshToken>(new CommandDefinition(sql, new { Token = token }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            INSERT INTO dbo.RefreshToken (Id, UserId, Token, ExpiresAtUtc, CreatedAtUtc)
            VALUES (@Id, @UserId, @Token, @ExpiresAtUtc, @CreatedAtUtc)";
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                refreshToken.Id,
                refreshToken.UserId,
                refreshToken.Token,
                refreshToken.ExpiresAtUtc,
                refreshToken.CreatedAtUtc
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return refreshToken.Id;
        }
    }

    public async Task RevokeAsync(Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.RefreshToken
            SET RevokedAtUtc = GETUTCDATE(), RevokedReason = @Reason
            WHERE Id = @Id";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Reason = reason ?? "Revoked" }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using (connection)
        {
            connection.Open();
            const string sql = @"
            UPDATE dbo.RefreshToken
            SET RevokedAtUtc = GETUTCDATE(), RevokedReason = @Reason
            WHERE UserId = @UserId AND RevokedAtUtc IS NULL";
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, Reason = reason ?? "Revoked" }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}

using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid id, string? reason, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string? reason, CancellationToken cancellationToken = default);
}

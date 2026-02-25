using Bangkok.Application.Dto.Auth;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Options;

namespace Bangkok.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly Bangkok.Application.Configuration.JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IOptions<Bangkok.Application.Configuration.JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return null;

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = request.Role,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _userRepository.CreateAsync(user, cancellationToken).ConfigureAwait(false);

        var (refreshTokenValue, refreshExpires) = _jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken).ConfigureAwait(false);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer"
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return null;

        var (refreshTokenValue, refreshExpires) = _jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken).ConfigureAwait(false);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer"
        };
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
        if (stored == null || stored.ExpiresAtUtc < DateTime.UtcNow)
            return null;

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return null;

        await _refreshTokenRepository.RevokeAsync(stored.Id, "Refreshed", cancellationToken).ConfigureAwait(false);

        var (refreshTokenValue, refreshExpires) = _jwtService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken).ConfigureAwait(false);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer"
        };
    }

    public async Task<bool> RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
        if (stored == null)
            return false;

        await _refreshTokenRepository.RevokeAsync(stored.Id, "User revoke", cancellationToken).ConfigureAwait(false);
        return true;
    }
}

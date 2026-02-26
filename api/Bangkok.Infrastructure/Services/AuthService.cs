using System.Security.Cryptography;
using Bangkok.Application.Dto.Auth;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bangkok.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int RecoveryTokenBytes = 32;
    private const int RecoveryExpiryHours = 1;
    private const int LockoutFailedAttemptLimit = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly Bangkok.Application.Configuration.JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogger _audit;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IOptions<Bangkok.Application.Configuration.JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IAuditLogger audit)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _audit = audit;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return null;

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = request.Role,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _userRepository.CreateAsync(user, cancellationToken).ConfigureAwait(false);
        _audit.LogUserCreated(user.Id, user.Email, clientIp);

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
            TokenType = "Bearer",
            ApplicationId = user.Id.ToString(),
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            _audit.LogLoginFailure(request.Email, clientIp);
            return LoginResult.Failed();
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Locked account login attempt. Email: {Email}, ClientIp: {ClientIp}, Timestamp: {Timestamp:O}", user.Email, clientIp ?? "unknown", DateTime.UtcNow);
            return LoginResult.Locked();
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.FailedLoginAttempts++;
            var lockoutTriggered = false;
            if (user.FailedLoginAttempts >= LockoutFailedAttemptLimit)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
                lockoutTriggered = true;
                _audit.LogAccountLockoutTriggered(user.Email, clientIp);
            }
            if (!lockoutTriggered)
                _audit.LogLoginFailure(user.Email, clientIp);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            return LoginResult.Failed();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

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

        _audit.LogLoginSuccess(user.Id, user.Email, clientIp);

        return LoginResult.Succeeded(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer",
            DisplayName = user.DisplayName,
            Role = user.Role
        });
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
            TokenType = "Bearer",
            ApplicationId = user.Id.ToString(),
            DisplayName = user.DisplayName,
            Role = user.Role
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forgot password request received");
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return;

        var recoverStringBytes = new byte[RecoveryTokenBytes];
        RandomNumberGenerator.Fill(recoverStringBytes);
        var recoverString = Convert.ToBase64String(recoverStringBytes);

        user.RecoverString = recoverString;
        user.RecoverStringExpiry = DateTime.UtcNow.AddHours(RecoveryExpiryHours);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRecoverStringAsync(request.RecoverString, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Password reset failed: recovery token not found");
            return false;
        }

        if (user.RecoverStringExpiry == null || user.RecoverStringExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset failed: recovery token expired");
            return false;
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.RecoverString = null;
        user.RecoverStringExpiry = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Password reset completed successfully");
        return true;
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return ChangePasswordResult.NotFound;

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("Change password failed: invalid current password. UserId: {UserId}", userId);
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Password changed successfully. UserId: {UserId}", userId);
        return ChangePasswordResult.Success;
    }
}

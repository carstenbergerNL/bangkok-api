using Bangkok.Application.Dto.Auth;

namespace Bangkok.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default, string? clientIp = null);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default, string? clientIp = null);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

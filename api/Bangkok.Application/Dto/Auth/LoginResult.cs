namespace Bangkok.Application.Dto.Auth;

/// <summary>
/// Result of a login attempt. Used to distinguish success, invalid credentials, and account locked.
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public bool IsLocked { get; set; }
    public AuthResponse? AuthResponse { get; set; }

    public static LoginResult Succeeded(AuthResponse response) => new()
    {
        Success = true,
        AuthResponse = response
    };

    public static LoginResult Failed() => new() { Success = false, IsLocked = false };

    public static LoginResult Locked() => new() { Success = false, IsLocked = true };
}

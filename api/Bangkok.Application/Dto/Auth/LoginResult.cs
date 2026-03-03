using Bangkok.Application.Dto;

namespace Bangkok.Application.Dto.Auth;

/// <summary>
/// Result of a login attempt. Used to distinguish success, invalid credentials, account locked, and tenant selection required.
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public bool IsLocked { get; set; }
    /// <summary>When true, client must send login again with TenantId from Tenants.</summary>
    public bool TenantRequired { get; set; }
    public string? Message { get; set; }
    public AuthResponse? AuthResponse { get; set; }
    public IReadOnlyList<TenantResponse>? Tenants { get; set; }

    public static LoginResult Succeeded(AuthResponse response) => new()
    {
        Success = true,
        AuthResponse = response
    };

    public static LoginResult Failed(string? message = null) => new() { Success = false, IsLocked = false, Message = message };

    public static LoginResult Locked() => new() { Success = false, IsLocked = true };

    public static LoginResult TenantSelectionRequired(IReadOnlyList<TenantResponse> tenants) => new()
    {
        Success = false,
        TenantRequired = true,
        Tenants = tenants
    };
}

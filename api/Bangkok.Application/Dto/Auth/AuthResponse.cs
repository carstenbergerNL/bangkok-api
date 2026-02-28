namespace Bangkok.Application.Dto.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
    /// <summary>User id (Guid) for API calls that require the current user, e.g. profile.</summary>
    public string? ApplicationId { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>Roles assigned to the user.</summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    /// <summary>Permission names from the user's roles (e.g. ViewAdminSettings).</summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

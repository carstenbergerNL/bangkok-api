namespace Bangkok.Application.Dto.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}

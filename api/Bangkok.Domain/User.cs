namespace Bangkok.Domain;

/// <summary>
/// User entity. Primary key is UNIQUEIDENTIFIER (Guid).
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? RecoverString { get; set; }
    public DateTime? RecoverStringExpiry { get; set; }
}

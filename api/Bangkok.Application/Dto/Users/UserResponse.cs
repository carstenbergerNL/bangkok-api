namespace Bangkok.Application.Dto.Users;

/// <summary>
/// Safe user projection; excludes PasswordHash, PasswordSalt, RecoverString, RecoverStringExpiry.
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

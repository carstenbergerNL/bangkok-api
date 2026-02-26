namespace Bangkok.Application.Dto.Profile;

/// <summary>
/// Profile response DTO; includes AvatarBase64 when present.
/// </summary>
public class ProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarBase64 { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Profile;

public class CreateProfileDto
{
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Optional avatar as base64 string (data URL prefix will be stripped). Max ~2MB decoded; jpeg/png only.
    /// </summary>
    public string? AvatarBase64 { get; set; }
}

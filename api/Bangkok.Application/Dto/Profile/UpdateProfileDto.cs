using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Profile;

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Optional avatar as base64 string. Max ~2MB decoded; jpeg/png only. Set to empty string to clear.
    /// </summary>
    public string? AvatarBase64 { get; set; }
}

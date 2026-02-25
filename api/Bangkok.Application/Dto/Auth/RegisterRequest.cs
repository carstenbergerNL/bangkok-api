using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    public string Role { get; set; } = "User";
}

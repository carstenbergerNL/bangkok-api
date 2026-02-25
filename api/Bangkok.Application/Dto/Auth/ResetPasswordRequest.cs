using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string RecoverString { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

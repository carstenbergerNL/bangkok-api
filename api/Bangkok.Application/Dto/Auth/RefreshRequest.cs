using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

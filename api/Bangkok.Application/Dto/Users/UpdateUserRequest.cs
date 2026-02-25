using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Users;

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}

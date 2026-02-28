using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Roles;

public class CreateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Permissions;

public class CreatePermissionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

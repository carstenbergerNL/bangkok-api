using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Permissions;

public class UpdatePermissionRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}

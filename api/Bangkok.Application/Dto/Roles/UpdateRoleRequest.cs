using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Roles;

public class UpdateRoleRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}

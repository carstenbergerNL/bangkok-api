using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class UpdateProjectRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class UpdateProjectTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public IReadOnlyList<CreateProjectTemplateTaskRequest>? Tasks { get; set; }
}

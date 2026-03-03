using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class CreateProjectTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public IReadOnlyList<CreateProjectTemplateTaskRequest>? Tasks { get; set; }
}

public class CreateProjectTemplateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? DefaultStatus { get; set; }

    [MaxLength(50)]
    public string? DefaultPriority { get; set; }
}

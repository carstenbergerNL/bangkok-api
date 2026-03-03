using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class CreateLabelRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Color { get; set; } = "#6366f1";
}

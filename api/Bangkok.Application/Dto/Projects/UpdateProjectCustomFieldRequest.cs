using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class UpdateProjectCustomFieldRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text";

    [MaxLength(8000)]
    public string? Options { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Projects;

public class CreateProjectCustomFieldRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = "Text"; // Text, Number, Date, Dropdown

    [MaxLength(8000)]
    public string? Options { get; set; } // For Dropdown: JSON array string or comma-separated
}

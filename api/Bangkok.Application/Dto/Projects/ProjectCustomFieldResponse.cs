namespace Bangkok.Application.Dto.Projects;

public class ProjectCustomFieldResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty; // Text, Number, Date, Dropdown
    public string? Options { get; set; }
    public DateTime CreatedAt { get; set; }
}

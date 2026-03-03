namespace Bangkok.Domain;

/// <summary>
/// Custom field definition for a project. Tasks can have values for these fields.
/// </summary>
public class ProjectCustomField
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty; // Text, Number, Date, Dropdown
    public string? Options { get; set; } // JSON array for Dropdown, e.g. ["A","B"]
    public DateTime CreatedAt { get; set; }
}

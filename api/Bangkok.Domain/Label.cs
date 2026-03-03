namespace Bangkok.Domain;

/// <summary>
/// Label entity. Belongs to a Project. Used as tags on tasks.
/// </summary>
public class Label
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}

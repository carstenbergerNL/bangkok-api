namespace Bangkok.Domain;

/// <summary>
/// Standalone task (tenant-scoped). Not tied to Projects.
/// </summary>
public class TasksStandalone
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public Guid? AssignedToUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

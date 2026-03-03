namespace Bangkok.Domain;

/// <summary>
/// Task entity. Primary key is UNIQUEIDENTIFIER (Guid). Belongs to a Project.
/// </summary>
public class ProjectTask
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? EstimatedHours { get; set; }
}

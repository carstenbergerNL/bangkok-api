namespace Bangkok.Application.Dto.TasksStandalone;

public class TasksStandaloneResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToDisplayName { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

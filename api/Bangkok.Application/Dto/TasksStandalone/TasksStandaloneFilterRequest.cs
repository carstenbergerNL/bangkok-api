namespace Bangkok.Application.Dto.TasksStandalone;

/// <summary>
/// Query filters for listing standalone tasks.
/// </summary>
public class TasksStandaloneFilterRequest
{
    public string? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueBefore { get; set; }
    public string? Search { get; set; }
}

namespace Bangkok.Application.Dto.Tasks;

/// <summary>
/// Optional query parameters for filtering tasks by project.
/// </summary>
public class TaskFilterRequest
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? LabelId { get; set; }
    public DateTime? DueBefore { get; set; }
    public DateTime? DueAfter { get; set; }
    public string? Search { get; set; }
}

using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Dto.Tasks;

public class TaskResponse
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
    public IReadOnlyList<LabelResponse> Labels { get; set; } = Array.Empty<LabelResponse>();
}

using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Tasks;

public class UpdateTaskRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Priority { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public bool? IsRecurring { get; set; }
    [MaxLength(100)]
    public string? RecurrencePattern { get; set; }
    [Range(1, 999)]
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public IReadOnlyList<Guid>? LabelIds { get; set; }
    public IReadOnlyList<TaskCustomFieldValueItem>? CustomFieldValues { get; set; }
}

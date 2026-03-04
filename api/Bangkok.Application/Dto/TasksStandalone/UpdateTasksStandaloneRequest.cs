using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.TasksStandalone;

public class UpdateTasksStandaloneRequest
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
}

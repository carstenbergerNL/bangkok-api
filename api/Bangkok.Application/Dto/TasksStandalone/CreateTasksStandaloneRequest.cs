using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.TasksStandalone;

public class CreateTasksStandaloneRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Open";

    [MaxLength(50)]
    public string Priority { get; set; } = "Medium";

    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; }
}

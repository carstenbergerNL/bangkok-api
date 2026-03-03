namespace Bangkok.Application.Dto.Tasks;

public class TaskTimeLogResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

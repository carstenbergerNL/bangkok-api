namespace Bangkok.Domain;

public class TaskTimeLog
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

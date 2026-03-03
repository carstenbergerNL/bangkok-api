namespace Bangkok.Application.Dto.Projects;

public class ProjectMemberResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Owner, Member, Viewer
    public DateTime CreatedAt { get; set; }
}

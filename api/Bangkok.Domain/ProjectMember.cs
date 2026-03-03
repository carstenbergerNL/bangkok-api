namespace Bangkok.Domain;

/// <summary>
/// Project membership for project-level access control. Role: Owner | Member | Viewer.
/// </summary>
public class ProjectMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty; // Owner, Member, Viewer
    public DateTime CreatedAt { get; set; }
}

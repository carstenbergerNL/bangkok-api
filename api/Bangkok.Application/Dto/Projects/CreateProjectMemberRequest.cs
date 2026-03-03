namespace Bangkok.Application.Dto.Projects;

public class CreateProjectMemberRequest
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member"; // Owner, Member, Viewer
}

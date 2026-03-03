namespace Bangkok.Application.Dto.Projects;

public class UpdateProjectMemberRequest
{
    public string Role { get; set; } = "Member"; // Owner, Member, Viewer
}

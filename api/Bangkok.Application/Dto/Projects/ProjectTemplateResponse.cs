namespace Bangkok.Application.Dto.Projects;

public class ProjectTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<ProjectTemplateTaskResponse> Tasks { get; set; } = Array.Empty<ProjectTemplateTaskResponse>();
}

namespace Bangkok.Application.Dto.Projects;

public class ProjectTemplateTaskResponse
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultStatus { get; set; }
    public string? DefaultPriority { get; set; }
}

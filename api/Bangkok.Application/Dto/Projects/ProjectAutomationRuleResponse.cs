namespace Bangkok.Application.Dto.Projects;

public class ProjectAutomationRuleResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetValue { get; set; }
}

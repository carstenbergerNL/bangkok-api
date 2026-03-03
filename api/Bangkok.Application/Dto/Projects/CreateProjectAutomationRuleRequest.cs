namespace Bangkok.Application.Dto.Projects;

public class CreateProjectAutomationRuleRequest
{
    public string Trigger { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetValue { get; set; }
}

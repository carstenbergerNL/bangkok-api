namespace Bangkok.Domain;

/// <summary>
/// Lightweight automation rule: when Trigger happens, run Action.
/// Triggers: TaskCompleted, TaskOverdue, TaskAssigned.
/// Actions: NotifyUser (TargetUserId), ChangeStatus (TargetValue = status), AddLabel (TargetValue = label id).
/// </summary>
public class ProjectAutomationRule
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetValue { get; set; }
}

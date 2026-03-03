namespace Bangkok.Application.Dto.Billing;

public class SubscriptionUsageResponse
{
    public PlanResponse? Plan { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ProjectsUsed { get; set; }
    public int? ProjectsLimit { get; set; }
    public int MembersUsed { get; set; }
    public int? MembersLimit { get; set; }
    public decimal StorageUsedMB { get; set; }
    public decimal? StorageLimitMB { get; set; }
    public int TimeLogsUsed { get; set; }
    public bool AutomationEnabled { get; set; }
}

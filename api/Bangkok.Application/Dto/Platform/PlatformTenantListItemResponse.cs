namespace Bangkok.Application.Dto.Platform;

public class PlatformTenantListItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? PlanName { get; set; }
    public string? SubscriptionStatus { get; set; }
    public int ProjectsCount { get; set; }
    public int UsersCount { get; set; }
    public decimal StorageUsedMB { get; set; }
    public int TimeLogsCount { get; set; }
}

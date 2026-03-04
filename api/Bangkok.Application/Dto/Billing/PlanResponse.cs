namespace Bangkok.Application.Dto.Billing;

public class PlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public int? MaxProjects { get; set; }
    public int? MaxUsers { get; set; }
    public bool AutomationEnabled { get; set; }
    public string? StripePriceIdMonthly { get; set; }
    public string? StripePriceIdYearly { get; set; }
    public decimal? StorageLimitMB { get; set; }
    public int? MaxStandaloneTasks { get; set; }
}

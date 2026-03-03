namespace Bangkok.Application.Dto.Platform;

public class PlatformDashboardStatsResponse
{
    public int TotalTenants { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public int TrialUsers { get; set; }
    public int ChurnedUsers { get; set; }
}

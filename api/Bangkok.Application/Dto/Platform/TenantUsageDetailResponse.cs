namespace Bangkok.Application.Dto.Platform;

public class TenantUsageDetailResponse
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int ProjectsCount { get; set; }
    public int UsersCount { get; set; }
    public decimal StorageUsedMB { get; set; }
    public int TimeLogsCount { get; set; }
}

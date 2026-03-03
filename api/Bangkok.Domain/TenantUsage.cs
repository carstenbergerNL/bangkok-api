namespace Bangkok.Domain;

/// <summary>
/// Tracked usage per tenant. Updated on project create/delete, member add/remove, file upload/delete, time log create/delete.
/// </summary>
public class TenantUsage
{
    public Guid TenantId { get; set; }
    public int ProjectsCount { get; set; }
    public int UsersCount { get; set; }
    public decimal StorageUsedMB { get; set; }
    public int TimeLogsCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

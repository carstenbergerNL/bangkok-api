namespace Bangkok.Domain;

/// <summary>
/// Tracks which modules are active for a tenant.
/// </summary>
public class TenantModule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsActive { get; set; }
}

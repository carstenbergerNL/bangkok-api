namespace Bangkok.Domain;

/// <summary>
/// User-level access to a module within a tenant. Tenant Admin grants/revokes.
/// </summary>
public class TenantModuleUser
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

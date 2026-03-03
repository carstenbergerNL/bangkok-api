namespace Bangkok.Domain;

/// <summary>
/// User membership in a tenant with a role (e.g. Admin, Member).
/// </summary>
public class TenantUser
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

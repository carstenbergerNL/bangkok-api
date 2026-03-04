namespace Bangkok.Application.Dto.TenantAdmin;

/// <summary>
/// User in the current tenant with role and module access. For GET /api/tenant-admin/users.
/// </summary>
public class TenantAdminUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string TenantRole { get; set; } = "Member";
    public IReadOnlyList<string> ActiveModules { get; set; } = Array.Empty<string>();
}

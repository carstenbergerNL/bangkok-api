namespace Bangkok.Application.Interfaces;

/// <summary>
/// User-level module access: grant/revoke per user. Tenant Admin only.
/// </summary>
public interface ITenantModuleUserService
{
    /// <summary>
    /// User IDs that have access to the module for the tenant. Tenant Admin only.
    /// </summary>
    Task<(bool Allowed, IReadOnlyList<ModuleAccessUserDto>? Users, string? Error)> GetUsersWithAccessAsync(Guid tenantId, string moduleKey, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant user access to the module. Validates: current user is Tenant Admin, target user in tenant, module active. Tenant Admin only.
    /// </summary>
    Task<(bool Success, string? Error)> GrantAccessAsync(Guid tenantId, string moduleKey, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke user access to the module. Tenant Admin only.
    /// </summary>
    Task<(bool Success, string? Error)> RevokeAccessAsync(Guid tenantId, string moduleKey, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);
}

public class ModuleAccessUserDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public bool HasAccess { get; set; }
}

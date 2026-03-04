namespace Bangkok.Application.Interfaces;

/// <summary>
/// Active modules for current tenant and admin management.
/// </summary>
public interface ITenantModuleService
{
    /// <summary>
    /// Returns module keys the given user can access for the current tenant (active for tenant + user-level access). Used by sidebar. Pass current user id from controller.
    /// </summary>
    Task<IReadOnlyList<string>> GetActiveModuleKeysForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the given module key is active for the current tenant (or user is platform admin).
    /// </summary>
    Task<bool> IsModuleActiveAsync(string moduleKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if user has access to the module: user in tenant, module active for tenant, user has entry in TenantModuleUser (or no user-level list for that module).
    /// </summary>
    Task<bool> HasModuleAccessAsync(Guid userId, Guid tenantId, string moduleKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// All modules with active flag for current tenant. For admin "Manage Modules" UI.
    /// </summary>
    Task<IReadOnlyList<TenantModuleListItem>> GetTenantModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set module active/inactive for current tenant. Requires platform admin or tenant admin.
    /// </summary>
    Task<(bool Success, string? Error)> SetModuleActiveAsync(string moduleKey, bool isActive, CancellationToken cancellationToken = default);
}

public class TenantModuleListItem
{
    public Guid TenantModuleId { get; set; }
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

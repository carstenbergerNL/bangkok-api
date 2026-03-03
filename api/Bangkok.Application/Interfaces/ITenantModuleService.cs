namespace Bangkok.Application.Interfaces;

/// <summary>
/// Active modules for current tenant and admin management.
/// </summary>
public interface ITenantModuleService
{
    /// <summary>
    /// Returns module keys that are active for the current tenant (from context).
    /// Used by frontend sidebar and by middleware to allow/deny access.
    /// </summary>
    Task<IReadOnlyList<string>> GetActiveModuleKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the given module key is active for the current tenant (or user is platform admin).
    /// </summary>
    Task<bool> IsModuleActiveAsync(string moduleKey, CancellationToken cancellationToken = default);

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

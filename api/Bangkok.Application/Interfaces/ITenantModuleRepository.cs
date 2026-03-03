using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITenantModuleRepository
{
    Task<TenantModule?> GetAsync(Guid tenantId, Guid moduleId, CancellationToken cancellationToken = default);
    Task<bool> IsModuleActiveAsync(Guid tenantId, string moduleKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActiveModuleKeysAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantModule>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid tenantId, Guid moduleId, bool isActive, CancellationToken cancellationToken = default);
    Task EnsureTenantModuleAsync(Guid tenantId, Guid moduleId, bool isActive, CancellationToken cancellationToken = default);
}

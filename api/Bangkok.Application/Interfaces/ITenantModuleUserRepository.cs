using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITenantModuleUserRepository
{
    Task<bool> ExistsAsync(Guid tenantId, Guid moduleId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetUserIdsWithAccessAsync(Guid tenantId, Guid moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActiveModuleKeysForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetModuleIdsForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveAllForUserInTenantAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(TenantModuleUser entity, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid tenantId, Guid moduleId, Guid userId, CancellationToken cancellationToken = default);
}

using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITenantUserRepository
{
    Task<IReadOnlyList<TenantUser>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TenantUser?> GetByTenantAndUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TenantUser tenantUser, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateRoleAsync(Guid tenantId, Guid userId, string role, CancellationToken cancellationToken = default);
    Task<int> CountAdminsInTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

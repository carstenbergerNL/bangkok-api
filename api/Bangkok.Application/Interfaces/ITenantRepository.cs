using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateStripeCustomerIdAsync(Guid tenantId, string? stripeCustomerId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid tenantId, string status, CancellationToken cancellationToken = default);
}

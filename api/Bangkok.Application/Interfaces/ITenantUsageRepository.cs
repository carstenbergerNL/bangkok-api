using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Tenant usage tracking. One row per tenant; updated on project/member/storage/timelog changes.
/// </summary>
public interface ITenantUsageRepository
{
    Task<TenantUsage?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Ensures a row exists for the tenant (e.g. on first project create). Idempotent.</summary>
    Task EnsureExistsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task IncrementProjectsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DecrementProjectsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task IncrementUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DecrementUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddStorageMbAsync(Guid tenantId, decimal mb, CancellationToken cancellationToken = default);
    Task RemoveStorageMbAsync(Guid tenantId, decimal mb, CancellationToken cancellationToken = default);
    Task IncrementTimeLogsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DecrementTimeLogsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantUsage>> GetAllAsync(CancellationToken cancellationToken = default);
}

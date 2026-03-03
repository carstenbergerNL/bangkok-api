using Bangkok.Application.Dto.Platform;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Platform (Super Admin) dashboard: stats, tenant list, suspend, upgrade, usage.
/// </summary>
public interface IPlatformAdminService
{
    Task<PlatformDashboardStatsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformTenantListItemResponse>> GetTenantsAsync(CancellationToken cancellationToken = default);
    Task<TenantUsageDetailResponse?> GetTenantUsageAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> SetTenantStatusAsync(Guid tenantId, string status, CancellationToken cancellationToken = default);
    Task<bool> UpgradeTenantAsync(Guid tenantId, Guid planId, CancellationToken cancellationToken = default);
}

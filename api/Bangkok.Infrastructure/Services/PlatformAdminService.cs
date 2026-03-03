using Bangkok.Application.Dto.Platform;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;

namespace Bangkok.Infrastructure.Services;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly ITenantUsageRepository _usageRepository;
    private readonly IPlanRepository _planRepository;

    public PlatformAdminService(
        ITenantRepository tenantRepository,
        ITenantSubscriptionRepository subscriptionRepository,
        ITenantUsageRepository usageRepository,
        IPlanRepository planRepository)
    {
        _tenantRepository = tenantRepository;
        _subscriptionRepository = subscriptionRepository;
        _usageRepository = usageRepository;
        _planRepository = planRepository;
    }

    public async Task<PlatformDashboardStatsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var (activeCount, trialCount, churnedCount, mrr) = await _subscriptionRepository.GetSubscriptionStatsAsync(cancellationToken).ConfigureAwait(false);

        return new PlatformDashboardStatsResponse
        {
            TotalTenants = tenants.Count,
            ActiveSubscriptions = activeCount,
            MonthlyRecurringRevenue = mrr,
            TrialUsers = trialCount,
            ChurnedUsers = churnedCount
        };
    }

    public async Task<IReadOnlyList<PlatformTenantListItemResponse>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var usageList = await _usageRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var usageByTenant = usageList.ToDictionary(u => u.TenantId, u => u);
        var subsWithPlan = await _subscriptionRepository.GetCurrentSubscriptionWithPlanForAllTenantsAsync(cancellationToken).ConfigureAwait(false);
        var subByTenant = subsWithPlan.ToDictionary(x => x.TenantId, x => (x.PlanName, x.SubscriptionStatus));

        return tenants.Select(t =>
        {
            var usage = usageByTenant.GetValueOrDefault(t.Id);
            var sub = subByTenant.GetValueOrDefault(t.Id);
            return new PlatformTenantListItemResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Status = t.Status,
                PlanName = sub.PlanName,
                SubscriptionStatus = sub.SubscriptionStatus,
                ProjectsCount = usage?.ProjectsCount ?? 0,
                UsersCount = usage?.UsersCount ?? 0,
                StorageUsedMB = usage?.StorageUsedMB ?? 0,
                TimeLogsCount = usage?.TimeLogsCount ?? 0
            };
        }).ToList();
    }

    public async Task<TenantUsageDetailResponse?> GetTenantUsageAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null) return null;

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return new TenantUsageDetailResponse
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Slug = tenant.Slug,
            Status = tenant.Status,
            ProjectsCount = usage?.ProjectsCount ?? 0,
            UsersCount = usage?.UsersCount ?? 0,
            StorageUsedMB = usage?.StorageUsedMB ?? 0,
            TimeLogsCount = usage?.TimeLogsCount ?? 0
        };
    }

    public async Task<bool> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null) return false;

        await _tenantRepository.UpdateStatusAsync(tenantId, "Suspended", cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetTenantStatusAsync(Guid tenantId, string status, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null) return false;

        await _tenantRepository.UpdateStatusAsync(tenantId, status, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> UpgradeTenantAsync(Guid tenantId, Guid planId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null) return false;

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan == null) return false;

        var sub = await _subscriptionRepository.GetCurrentByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (sub == null)
        {
            var newSub = new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanId = planId,
                Status = "Active",
                StartDate = DateTime.UtcNow,
                EndDate = null
            };
            await _subscriptionRepository.CreateAsync(newSub, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            sub.PlanId = planId;
            await _subscriptionRepository.UpdateAsync(sub, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }
}

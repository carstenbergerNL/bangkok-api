using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITenantSubscriptionRepository
{
    Task<TenantSubscription?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    /// <summary>Latest subscription by StartDate (any status). For manual upgrade.</summary>
    Task<TenantSubscription?> GetCurrentByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<TenantSubscription> CreateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>For platform dashboard: counts and MRR. Active/Trial = not ended; Churned = Cancelled or EndDate passed.</summary>
    Task<(int ActiveCount, int TrialCount, int ChurnedCount, decimal Mrr)> GetSubscriptionStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Current subscription per tenant (latest by StartDate) with plan name.</summary>
    Task<IReadOnlyList<(Guid TenantId, string PlanName, string SubscriptionStatus)>> GetCurrentSubscriptionWithPlanForAllTenantsAsync(CancellationToken cancellationToken = default);
}

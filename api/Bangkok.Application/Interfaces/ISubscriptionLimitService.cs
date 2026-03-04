using Bangkok.Application.Dto.Billing;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Checks subscription limits for the current tenant (create project, add member, automation).
/// </summary>
public interface ISubscriptionLimitService
{
    Task<SubscriptionUsageResponse?> GetUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns (allowed, errorMessage). If allowed, message is null.</summary>
    Task<(bool Allowed, string? LimitMessage)> CanCreateProjectAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns (allowed, errorMessage).</summary>
    Task<(bool Allowed, string? LimitMessage)> CanAddMemberAsync(CancellationToken cancellationToken = default);

    /// <summary>Check for a specific tenant (e.g. when adding user to default tenant on register).</summary>
    Task<(bool Allowed, string? LimitMessage)> CanAddMemberForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns (allowed, errorMessage). Check storage limit before upload.</summary>
    Task<(bool Allowed, string? LimitMessage)> CanAddStorageAsync(Guid tenantId, decimal additionalMb, CancellationToken cancellationToken = default);

    /// <summary>Returns (allowed, errorMessage).</summary>
    Task<(bool Allowed, string? LimitMessage)> CanUseAutomationAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns (allowed, errorMessage). Enforce max standalone tasks per plan (e.g. Free 100, Pro unlimited).</summary>
    Task<(bool Allowed, string? LimitMessage)> CanCreateStandaloneTaskAsync(CancellationToken cancellationToken = default);
}

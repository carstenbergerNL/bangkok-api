using Bangkok.Application.Dto.Billing;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;

namespace Bangkok.Infrastructure.Services;

public class SubscriptionLimitService : ISubscriptionLimitService
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantUsageRepository _usageRepository;

    public SubscriptionLimitService(
        ITenantContext tenantContext,
        ITenantSubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IProjectRepository projectRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantUsageRepository usageRepository)
    {
        _tenantContext = tenantContext;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _projectRepository = projectRepository;
        _tenantUserRepository = tenantUserRepository;
        _usageRepository = usageRepository;
    }

    public async Task<SubscriptionUsageResponse?> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return null;

        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (sub == null)
            return new SubscriptionUsageResponse { Status = "None", ProjectsUsed = 0, MembersUsed = 0, StorageUsedMB = 0, TimeLogsUsed = 0 };

        var plan = await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false);
        var usage = await _usageRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var projectsUsed = usage?.ProjectsCount ?? (await _projectRepository.GetAllAsync(tenantId, null, cancellationToken).ConfigureAwait(false)).Count;
        var membersUsed = usage?.UsersCount ?? (await _tenantUserRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)).Count;

        return new SubscriptionUsageResponse
        {
            Plan = plan == null ? null : MapPlan(plan),
            Status = sub.Status,
            StartDate = sub.StartDate,
            EndDate = sub.EndDate,
            ProjectsUsed = projectsUsed,
            ProjectsLimit = plan?.MaxProjects,
            MembersUsed = membersUsed,
            MembersLimit = plan?.MaxUsers,
            StorageUsedMB = usage?.StorageUsedMB ?? 0,
            StorageLimitMB = plan?.StorageLimitMB,
            TimeLogsUsed = usage?.TimeLogsCount ?? 0,
            AutomationEnabled = plan?.AutomationEnabled ?? false
        };
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanCreateProjectAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (true, null);

        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.MaxProjects == null)
            return (true, null);

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var count = usage?.ProjectsCount ?? (await _projectRepository.GetAllAsync(tenantId, null, cancellationToken).ConfigureAwait(false)).Count;
        if (count >= plan.MaxProjects.Value)
            return (false, $"Project limit reached ({plan.MaxProjects} projects). Upgrade your plan to add more.");
        return (true, null);
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanAddMemberAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (true, null);

        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.MaxUsers == null)
            return (true, null);

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var count = usage?.UsersCount ?? (await _tenantUserRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)).Count;
        if (count >= plan.MaxUsers.Value)
            return (false, $"Member limit reached ({plan.MaxUsers} members). Upgrade your plan to add more.");
        return (true, null);
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanAddStorageAsync(Guid tenantId, decimal additionalMb, CancellationToken cancellationToken = default)
    {
        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.StorageLimitMB == null)
            return (true, null);

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var currentMb = usage?.StorageUsedMB ?? 0;
        if (currentMb + additionalMb > plan.StorageLimitMB.Value)
            return (false, $"Storage limit reached ({plan.StorageLimitMB} MB). Upgrade your plan for more storage.");
        return (true, null);
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanUseAutomationAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (true, null);

        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.AutomationEnabled == true)
            return (true, null);
        return (false, "Automation is not included in your current plan. Upgrade to enable automation rules.");
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanCreateStandaloneTaskAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (true, null);

        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.MaxStandaloneTasks == null)
            return (true, null);

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var count = usage?.StandaloneTasksCount ?? 0;
        if (count >= plan.MaxStandaloneTasks.Value)
            return (false, $"Task limit reached ({plan.MaxStandaloneTasks} tasks). Upgrade your plan to add more.");
        return (true, null);
    }

    public async Task<(bool Allowed, string? LimitMessage)> CanAddMemberForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sub = await _subscriptionRepository.GetActiveByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var plan = sub != null ? await _planRepository.GetByIdAsync(sub.PlanId, cancellationToken).ConfigureAwait(false) : null;
        if (plan?.MaxUsers == null)
            return (true, null);

        var usage = await _usageRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var count = usage?.UsersCount ?? (await _tenantUserRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false)).Count;
        if (count >= plan.MaxUsers.Value)
            return (false, $"Member limit reached ({plan.MaxUsers} members). Upgrade your plan to add more.");
        return (true, null);
    }

    private static PlanResponse MapPlan(Plan p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        PriceMonthly = p.PriceMonthly,
        PriceYearly = p.PriceYearly,
        MaxProjects = p.MaxProjects,
        MaxUsers = p.MaxUsers,
        AutomationEnabled = p.AutomationEnabled,
        StripePriceIdMonthly = p.StripePriceIdMonthly,
        StripePriceIdYearly = p.StripePriceIdYearly,
        StorageLimitMB = p.StorageLimitMB,
        MaxStandaloneTasks = p.MaxStandaloneTasks
    };
}

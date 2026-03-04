using Bangkok.Application.Interfaces;
using Bangkok.Domain;

namespace Bangkok.Infrastructure.Services;

public class TenantModuleUserService : ITenantModuleUserService
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantModuleUserRepository _tenantModuleUserRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IUserRepository _userRepository;

    public TenantModuleUserService(
        ITenantContext tenantContext,
        ITenantUserRepository tenantUserRepository,
        ITenantModuleUserRepository tenantModuleUserRepository,
        ITenantModuleRepository tenantModuleRepository,
        IModuleRepository moduleRepository,
        IUserRepository userRepository)
    {
        _tenantContext = tenantContext;
        _tenantUserRepository = tenantUserRepository;
        _tenantModuleUserRepository = tenantModuleUserRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _moduleRepository = moduleRepository;
        _userRepository = userRepository;
    }

    public async Task<(bool Allowed, IReadOnlyList<ModuleAccessUserDto>? Users, string? Error)> GetUsersWithAccessAsync(Guid tenantId, string moduleKey, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsTenantAdminAsync(tenantId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "Only Tenant Admin can manage module access.");

        if (_tenantContext.CurrentTenantId != tenantId)
            return (false, null, "Tenant mismatch.");

        var module = await _moduleRepository.GetByKeyAsync(moduleKey, cancellationToken).ConfigureAwait(false);
        if (module == null)
            return (false, null, "Module not found.");

        var tenantModule = await _tenantModuleRepository.GetAsync(tenantId, module.Id, cancellationToken).ConfigureAwait(false);
        if (tenantModule == null || !tenantModule.IsActive)
            return (false, null, "Module is not active for this tenant.");

        var tenantUsers = await _tenantUserRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var userIds = tenantUsers.Select(tu => tu.UserId).ToList();
        var displayNames = await _userRepository.GetDisplayNamesByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        var usersWithAccess = await _tenantModuleUserRepository.GetUserIdsWithAccessAsync(tenantId, module.Id, cancellationToken).ConfigureAwait(false);
        var accessSet = usersWithAccess.ToHashSet();

        var list = tenantUsers.Select(tu =>
        {
            displayNames.TryGetValue(tu.UserId, out var name);
            return new ModuleAccessUserDto
            {
                UserId = tu.UserId,
                DisplayName = name,
                Email = null,
                HasAccess = accessSet.Contains(tu.UserId)
            };
        }).ToList();

        return (true, list, null);
    }

    public async Task<(bool Success, string? Error)> GrantAccessAsync(Guid tenantId, string moduleKey, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsTenantAdminAsync(tenantId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Only Tenant Admin can grant module access.");

        if (_tenantContext.CurrentTenantId != tenantId)
            return (false, "Tenant mismatch.");

        var module = await _moduleRepository.GetByKeyAsync(moduleKey, cancellationToken).ConfigureAwait(false);
        if (module == null)
            return (false, "Module not found.");

        if (!await _tenantModuleRepository.IsModuleActiveAsync(tenantId, moduleKey, cancellationToken).ConfigureAwait(false))
            return (false, "Module is not active for this tenant.");

        var targetMembership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        if (targetMembership == null)
            return (false, "User does not belong to this tenant.");

        var exists = await _tenantModuleUserRepository.ExistsAsync(tenantId, module.Id, userId, cancellationToken).ConfigureAwait(false);
        if (exists)
            return (true, null);

        await _tenantModuleUserRepository.AddAsync(new TenantModuleUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = module.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RevokeAccessAsync(Guid tenantId, string moduleKey, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsTenantAdminAsync(tenantId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Only Tenant Admin can revoke module access.");

        if (_tenantContext.CurrentTenantId != tenantId)
            return (false, "Tenant mismatch.");

        var module = await _moduleRepository.GetByKeyAsync(moduleKey, cancellationToken).ConfigureAwait(false);
        if (module == null)
            return (false, "Module not found.");

        await _tenantModuleUserRepository.RemoveAsync(tenantId, module.Id, userId, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    private async Task<bool> IsTenantAdminAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        if (_tenantContext.IsPlatformAdmin)
            return true;
        var membership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        return membership != null && string.Equals(membership.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}

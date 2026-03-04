using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Repositories;

namespace Bangkok.Infrastructure.Services;

public class TenantModuleService : ITenantModuleService
{
    private readonly ITenantContext _tenantContext;
    private readonly IModuleRepository _moduleRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly ITenantModuleUserRepository _tenantModuleUserRepository;

    public TenantModuleService(
        ITenantContext tenantContext,
        IModuleRepository moduleRepository,
        ITenantModuleRepository tenantModuleRepository,
        ITenantModuleUserRepository tenantModuleUserRepository)
    {
        _tenantContext = tenantContext;
        _moduleRepository = moduleRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _tenantModuleUserRepository = tenantModuleUserRepository;
    }

    public async Task<IReadOnlyList<string>> GetActiveModuleKeysForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return Array.Empty<string>();
        if (_tenantContext.IsPlatformAdmin)
            return await _tenantModuleRepository.GetActiveModuleKeysAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        return await _tenantModuleUserRepository.GetActiveModuleKeysForUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasModuleAccessAsync(Guid userId, Guid tenantId, string moduleKey, CancellationToken cancellationToken = default)
    {
        var module = await _moduleRepository.GetByKeyAsync(moduleKey, cancellationToken).ConfigureAwait(false);
        if (module == null)
            return false;
        var isActive = await _tenantModuleRepository.IsModuleActiveAsync(tenantId, moduleKey, cancellationToken).ConfigureAwait(false);
        if (!isActive)
            return false;
        var keysForUser = await _tenantModuleUserRepository.GetActiveModuleKeysForUserAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        return keysForUser.Contains(moduleKey);
    }

    public async Task<bool> IsModuleActiveAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        if (_tenantContext.IsPlatformAdmin)
            return true;
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return false;
        return await _tenantModuleRepository.IsModuleActiveAsync(tenantId.Value, moduleKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantModuleListItem>> GetTenantModulesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return Array.Empty<TenantModuleListItem>();

        var allModules = await _moduleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var tenantModules = await _tenantModuleRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var byModuleId = tenantModules.ToDictionary(tm => tm.ModuleId, tm => tm);

        return allModules.Select(m =>
        {
            var tm = byModuleId.GetValueOrDefault(m.Id);
            return new TenantModuleListItem
            {
                TenantModuleId = tm?.Id ?? Guid.Empty,
                ModuleId = m.Id,
                Name = m.Name,
                Key = m.Key,
                Description = m.Description,
                IsActive = tm?.IsActive ?? false
            };
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> SetModuleActiveAsync(string moduleKey, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return (false, "Tenant context is required.");
        var module = await _moduleRepository.GetByKeyAsync(moduleKey, cancellationToken).ConfigureAwait(false);
        if (module == null)
            return (false, "Module not found.");
        await _tenantModuleRepository.EnsureTenantModuleAsync(_tenantContext.CurrentTenantId!.Value, module.Id, isActive, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }
}

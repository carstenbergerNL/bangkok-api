using System.Security.Cryptography;
using Bangkok.Application.Dto.TenantAdmin;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TenantAdminService : ITenantAdminService
{
    private const string RoleAdmin = "Admin";
    private const string RoleMember = "Member";

    private readonly ITenantContext _tenantContext;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantModuleUserRepository _tenantModuleUserRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly ITenantUsageRepository _usageRepository;
    private readonly ILogger<TenantAdminService> _logger;

    public TenantAdminService(
        ITenantContext tenantContext,
        ITenantUserRepository tenantUserRepository,
        ITenantModuleUserRepository tenantModuleUserRepository,
        ITenantModuleRepository tenantModuleRepository,
        IModuleRepository moduleRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISubscriptionLimitService subscriptionLimitService,
        ITenantUsageRepository usageRepository,
        ILogger<TenantAdminService> logger)
    {
        _tenantContext = tenantContext;
        _tenantUserRepository = tenantUserRepository;
        _tenantModuleUserRepository = tenantModuleUserRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _moduleRepository = moduleRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _subscriptionLimitService = subscriptionLimitService;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<(bool Allowed, IReadOnlyList<TenantAdminUserResponse>? Users, string? Error)> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        if (!await IsTenantAdminOrPlatformAsync(tenantId.Value, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "Only Tenant Admin can manage users.");

        var tenantUsers = await _tenantUserRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var userIds = tenantUsers.Select(tu => tu.UserId).Distinct().ToList();
        if (userIds.Count == 0)
            return (true, new List<TenantAdminUserResponse>(), null);

        var displayNames = await _userRepository.GetDisplayNamesByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        var userEmails = await GetEmailsByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);

        var result = new List<TenantAdminUserResponse>();
        foreach (var tu in tenantUsers)
        {
            displayNames.TryGetValue(tu.UserId, out var displayName);
            userEmails.TryGetValue(tu.UserId, out var email);
            var moduleKeys = await _tenantModuleUserRepository.GetActiveModuleKeysForUserAsync(tenantId.Value, tu.UserId, cancellationToken).ConfigureAwait(false);
            result.Add(new TenantAdminUserResponse
            {
                UserId = tu.UserId,
                Email = email ?? string.Empty,
                DisplayName = displayName,
                TenantRole = tu.Role ?? RoleMember,
                ActiveModules = moduleKeys.ToList()
            });
        }

        return (true, result, null);
    }

    public async Task<(bool Success, TenantAdminUserResponse? User, string? Error)> InviteOrAddUserAsync(InviteTenantUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        if (!await IsTenantAdminOrPlatformAsync(tenantId.Value, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "Only Tenant Admin can add users.");

        var (canAdd, limitMsg) = await _subscriptionLimitService.CanAddMemberForTenantAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (!canAdd)
            return (false, null, limitMsg ?? "Cannot add more members.");

        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(email))
            return (false, null, "Email is required.");

        var role = NormalizeRole(request.TenantRole);
        var moduleKeys = request.ModuleKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k!.Trim()).ToList() ?? new List<string>();

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        Guid userId;
        if (existingUser != null)
        {
            userId = existingUser.Id;
            var existingMembership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
            if (existingMembership != null)
                return (false, null, "User is already in this tenant.");
        }
        else
        {
            var (hash, salt) = _passwordHasher.HashPassword(GenerateRandomPassword());
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = null,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user, cancellationToken).ConfigureAwait(false);
            userId = user.Id;
            _logger.LogInformation("Tenant admin created new user for invite. UserId: {UserId}, Email: {Email}", userId, email);
        }

        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            UserId = userId,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        await _tenantUserRepository.CreateAsync(tenantUser, cancellationToken).ConfigureAwait(false);
        await _usageRepository.EnsureExistsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        await _usageRepository.IncrementUsersAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        await SetUserModuleAccessAsync(tenantId.Value, userId, moduleKeys, cancellationToken).ConfigureAwait(false);

        var userDisplayName = existingUser != null ? (string.IsNullOrWhiteSpace(existingUser.DisplayName) ? null : existingUser.DisplayName) : null;
        var userEmail = existingUser?.Email ?? email;
        var activeModules = await _tenantModuleUserRepository.GetActiveModuleKeysForUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);

        return (true, new TenantAdminUserResponse
        {
            UserId = userId,
            Email = userEmail,
            DisplayName = userDisplayName,
            TenantRole = role,
            ActiveModules = activeModules.ToList()
        }, null);
    }

    public async Task<(bool Success, string? Error)> RemoveUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, "Tenant context required.");

        if (!await IsTenantAdminOrPlatformAsync(tenantId.Value, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Only Tenant Admin can remove users.");

        var membership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
        if (membership == null)
            return (false, "User not found in this tenant.");

        var adminCount = await _tenantUserRepository.CountAdminsInTenantAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (string.Equals(membership.Role, RoleAdmin, StringComparison.OrdinalIgnoreCase) && adminCount <= 1)
            return (false, "Cannot remove the last Tenant Admin.");

        await _tenantModuleUserRepository.RemoveAllForUserInTenantAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
        await _tenantUserRepository.DeleteAsync(membership.Id, cancellationToken).ConfigureAwait(false);
        await _usageRepository.DecrementUsersAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("User removed from tenant. UserId: {UserId}, TenantId: {TenantId}", userId, tenantId.Value);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserRoleAsync(Guid userId, string role, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, "Tenant context required.");

        if (!await IsTenantAdminOrPlatformAsync(tenantId.Value, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Only Tenant Admin can change roles.");

        var membership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
        if (membership == null)
            return (false, "User not found in this tenant.");

        var normalizedRole = NormalizeRole(role);
        if (string.Equals(membership.Role, RoleAdmin, StringComparison.OrdinalIgnoreCase) && !string.Equals(normalizedRole, RoleAdmin, StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _tenantUserRepository.CountAdminsInTenantAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
            if (adminCount <= 1)
                return (false, "Cannot demote the last Tenant Admin.");
        }

        await _tenantUserRepository.UpdateRoleAsync(tenantId.Value, userId, normalizedRole, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserModulesAsync(Guid userId, IReadOnlyList<string> moduleKeys, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, "Tenant context required.");

        if (!await IsTenantAdminOrPlatformAsync(tenantId.Value, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Only Tenant Admin can manage module access.");

        var membership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId.Value, userId, cancellationToken).ConfigureAwait(false);
        if (membership == null)
            return (false, "User not found in this tenant.");

        var keysList = moduleKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k!.Trim()).ToList() ?? new List<string>();
        await SetUserModuleAccessAsync(tenantId.Value, userId, keysList, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    private async Task<bool> IsTenantAdminOrPlatformAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        if (_tenantContext.IsPlatformAdmin)
            return true;
        var membership = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        return membership != null && string.Equals(membership.Role, RoleAdmin, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return RoleMember;
        return string.Equals(role.Trim(), RoleAdmin, StringComparison.OrdinalIgnoreCase) ? RoleAdmin : RoleMember;
    }

    private async Task SetUserModuleAccessAsync(Guid tenantId, Guid userId, IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken)
    {
        var activeKeys = await _tenantModuleRepository.GetActiveModuleKeysAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var allowedSet = activeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toGrant = moduleKeys.Where(k => allowedSet.Contains(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        await _tenantModuleUserRepository.RemoveAllForUserInTenantAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);

        foreach (var key in toGrant)
        {
            var module = await _moduleRepository.GetByKeyAsync(key, cancellationToken).ConfigureAwait(false);
            if (module == null) continue;
            if (!await _tenantModuleRepository.IsModuleActiveAsync(tenantId, key, cancellationToken).ConfigureAwait(false)) continue;

            await _tenantModuleUserRepository.AddAsync(new TenantModuleUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ModuleId = module.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GenerateRandomPassword()
    {
        var bytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        var dict = new Dictionary<Guid, string>();
        foreach (var id in ids)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user != null)
                dict[id] = user.Email;
        }
        return dict;
    }
}

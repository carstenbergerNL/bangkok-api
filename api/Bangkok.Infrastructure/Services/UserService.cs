using Bangkok.Application.Dto.Users;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IAuditLogger _audit;

    public UserService(IUserRepository userRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository, ILogger<UserService> logger, IAuditLogger audit)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _logger = logger;
        _audit = audit;
    }

    public async Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return user == null ? null : await MapToResponseAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetAllPagedAsync(pageNumber, pageSize, includeDeleted, cancellationToken).ConfigureAwait(false);
        var responses = new List<UserResponse>();
        foreach (var user in items)
            responses.Add(await MapToResponseAsync(user, cancellationToken).ConfigureAwait(false));
        return new PagedResult<UserResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<UpdateUserResult> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return UpdateUserResult.NotFound;

        var isSelf = id == currentUserId;

        if (!isSelf && !isAdmin)
        {
            _logger.LogWarning("Unauthorized user update attempt: user {CurrentUserId} tried to update user {TargetUserId}", currentUserId, id);
            return UpdateUserResult.Forbidden;
        }

        if (isSelf && !isAdmin)
        {
            if (request.IsActive.HasValue)
            {
                _logger.LogWarning("Unauthorized: non-admin user {UserId} attempted to change Role or IsActive", currentUserId);
                return UpdateUserResult.Forbidden;
            }
            var hasEmail = !string.IsNullOrWhiteSpace(request.Email);
            var hasDisplayName = request.DisplayName != null;
            if (!hasEmail && !hasDisplayName)
                return UpdateUserResult.BadRequest;
            if (hasEmail)
            {
                var newEmail = request.Email!.Trim();
                var existingByEmail = await _userRepository.GetByEmailAsync(newEmail, cancellationToken).ConfigureAwait(false);
                if (existingByEmail != null && existingByEmail.Id != id)
                    return UpdateUserResult.BadRequest;
                user.Email = newEmail;
            }
            if (hasDisplayName)
                user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            _audit.LogUserUpdated(id, user.Email, clientIp);
            return UpdateUserResult.Success;
        }

        if (isAdmin)
        {
            var roleChanged = false;
            if (request.Role != null)
            {
                await _userRoleRepository.RemoveAllForUserAsync(id, cancellationToken).ConfigureAwait(false);
                var roleByName = await _roleRepository.GetByNameAsync(request.Role, cancellationToken).ConfigureAwait(false);
                if (roleByName != null)
                {
                    await _userRoleRepository.AssignAsync(id, roleByName.Id, cancellationToken).ConfigureAwait(false);
                    roleChanged = true;
                }
            }
            var activeChanged = request.IsActive.HasValue && request.IsActive.Value != user.IsActive;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var newEmail = request.Email!.Trim();
                var existingByEmail = await _userRepository.GetByEmailAsync(newEmail, cancellationToken).ConfigureAwait(false);
                if (existingByEmail != null && existingByEmail.Id != id)
                    return UpdateUserResult.BadRequest;
                user.Email = newEmail;
            }
            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;
            if (request.DisplayName != null)
                user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

            if (roleChanged)
                _logger.LogInformation("Admin changed roles for user {UserId} to {Role}", id, request.Role);
            if (activeChanged && !user.IsActive)
                _logger.LogInformation("Admin deactivated user {UserId}", id);
            if (activeChanged && user.IsActive)
                _logger.LogInformation("Admin reactivated user {UserId}", id);
            if ((request.Email != null || request.DisplayName != null) && !roleChanged && !activeChanged)
                _logger.LogInformation("Admin updated profile for user {UserId}", id);

            return UpdateUserResult.Success;
        }

        return UpdateUserResult.Forbidden;
    }

    public async Task<DeleteUserResult> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        _logger.LogInformation("User delete attempt. TargetUserId: {TargetUserId}, ActorUserId: {ActorUserId}", id, currentUserId);
        var user = await _userRepository.GetByIdIncludeDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Delete failed: user not found. TargetUserId: {TargetUserId}", id);
            return DeleteUserResult.NotFound;
        }
        if (user.IsDeleted)
        {
            _logger.LogWarning("Delete failed: user already deleted. TargetUserId: {TargetUserId}", id);
            return DeleteUserResult.AlreadyDeleted;
        }
        if (id == currentUserId)
        {
            _logger.LogWarning("Unauthorized delete attempt: user tried to delete themselves. UserId: {UserId}", currentUserId);
            return DeleteUserResult.ForbiddenSelfDelete;
        }
        _audit.LogUserDeleted(id, user.Email, clientIp);
        await _userRepository.SoftDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("User soft-deleted successfully. UserId: {UserId}", id);
        return DeleteUserResult.Success;
    }

    public async Task<RestoreUserResult> RestoreUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User restore attempt. TargetUserId: {TargetUserId}", id);
        var user = await _userRepository.GetByIdIncludeDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Restore failed: user not found. TargetUserId: {TargetUserId}", id);
            return RestoreUserResult.NotFound;
        }
        if (!user.IsDeleted)
        {
            _logger.LogWarning("Restore failed: user is not deleted. TargetUserId: {TargetUserId}", id);
            return RestoreUserResult.NotDeleted;
        }
        await _userRepository.RestoreAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("User restored successfully. UserId: {UserId}", id);
        return RestoreUserResult.Success;
    }

    public async Task<HardDeleteUserResult> HardDeleteUserAsync(Guid id, Guid currentUserId, bool confirm, CancellationToken cancellationToken = default, string? clientIp = null)
    {
        _logger.LogInformation("Hard delete attempt. TargetUserId: {TargetUserId}, ActorUserId: {ActorUserId}, Confirm: {Confirm}, TimestampUtc: {TimestampUtc}",
            id, currentUserId, confirm, DateTime.UtcNow);
        if (!confirm)
        {
            _logger.LogWarning("Hard delete rejected: confirm parameter not true. TargetUserId: {TargetUserId}", id);
            return HardDeleteUserResult.ConfirmRequired;
        }
        var user = await _userRepository.GetByIdIncludeDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Hard delete failed: user not found. TargetUserId: {TargetUserId}", id);
            return HardDeleteUserResult.NotFound;
        }
        if (id == currentUserId)
        {
            _logger.LogWarning("Hard delete rejected: user attempted to delete themselves. ActorUserId: {ActorUserId}", currentUserId);
            return HardDeleteUserResult.ForbiddenSelfDelete;
        }
        _audit.LogUserDeleted(id, user.Email, clientIp);
        await _userRepository.HardDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("User hard-deleted permanently. TargetUserId: {TargetUserId}, ActorUserId: {ActorUserId}, TimestampUtc: {TimestampUtc}",
            id, currentUserId, DateTime.UtcNow);
        return HardDeleteUserResult.Success;
    }

    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public async Task<LockUserResult> LockUserAsync(Guid id, Guid currentUserId, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            _logger.LogWarning("Admin attempted to lock themselves. UserId: {UserId}", currentUserId);
            return LockUserResult.ForbiddenSelfLock;
        }
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Lock failed: user not found. TargetUserId: {TargetUserId}", id);
            return LockUserResult.NotFound;
        }

        var effectiveLockoutEnd = lockoutEnd.HasValue
            ? (lockoutEnd.Value.Kind == DateTimeKind.Utc ? lockoutEnd.Value : DateTime.SpecifyKind(lockoutEnd.Value, DateTimeKind.Utc))
            : (DateTime?)null;

        if (effectiveLockoutEnd.HasValue && effectiveLockoutEnd.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning("Lock failed: LockoutEnd must be in the future. TargetUserId: {TargetUserId}, LockoutEnd: {LockoutEnd:O}", id, effectiveLockoutEnd.Value);
            return LockUserResult.InvalidLockoutEnd;
        }

        var end = effectiveLockoutEnd ?? DateTime.UtcNow.Add(LockDuration);
        await _userRepository.SetLockoutAsync(id, end, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin locked user. TargetUserId: {TargetUserId}, LockoutEnd: {LockoutEnd:O}, ActorUserId: {ActorUserId}",
            id, end, currentUserId);
        return LockUserResult.Success;
    }

    public async Task<UnlockUserResult> UnlockUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Unlock failed: user not found. TargetUserId: {TargetUserId}", id);
            return UnlockUserResult.NotFound;
        }
        await _userRepository.ClearLockoutAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Admin unlocked user. TargetUserId: {TargetUserId}", id);
        return UnlockUserResult.Success;
    }

    private async Task<UserResponse> MapToResponseAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var roleNames = roles.Select(r => r.Name).ToList();
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roleNames,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            IsDeleted = user.IsDeleted,
            DeletedAtUtc = user.DeletedAt
        };
    }
}

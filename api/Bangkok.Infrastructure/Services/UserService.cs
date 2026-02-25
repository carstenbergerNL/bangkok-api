using Bangkok.Application.Dto.Users;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class UserService : IUserService
{
    private const string AdminRole = "Admin";

    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return user == null ? null : MapToResponse(user);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetAllPagedAsync(pageNumber, pageSize, cancellationToken).ConfigureAwait(false);
        return new PagedResult<UserResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<UpdateUserResult> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return UpdateUserResult.NotFound;

        var isAdmin = string.Equals(currentUserRole, AdminRole, StringComparison.OrdinalIgnoreCase);
        var isSelf = id == currentUserId;

        if (!isSelf && !isAdmin)
        {
            _logger.LogWarning("Unauthorized user update attempt: user {CurrentUserId} tried to update user {TargetUserId}", currentUserId, id);
            return UpdateUserResult.Forbidden;
        }

        if (isSelf && !isAdmin)
        {
            if (request.Role != null || request.IsActive.HasValue)
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
            _logger.LogInformation("User updated their own profile. UserId: {UserId}", id);
            return UpdateUserResult.Success;
        }

        if (isAdmin)
        {
            var roleChanged = request.Role != null && request.Role != user.Role;
            var activeChanged = request.IsActive.HasValue && request.IsActive.Value != user.IsActive;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var newEmail = request.Email!.Trim();
                var existingByEmail = await _userRepository.GetByEmailAsync(newEmail, cancellationToken).ConfigureAwait(false);
                if (existingByEmail != null && existingByEmail.Id != id)
                    return UpdateUserResult.BadRequest;
                user.Email = newEmail;
            }
            if (request.Role != null)
                user.Role = request.Role;
            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;
            if (request.DisplayName != null)
                user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

            if (roleChanged)
                _logger.LogInformation("Admin changed role for user {UserId} to {Role}", id, user.Role);
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

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }
}

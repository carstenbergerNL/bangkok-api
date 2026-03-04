using Bangkok.Application.Dto.TenantAdmin;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Tenant Admin: manage users and module access within the current tenant. Only Tenant Admin or Platform Admin.
/// </summary>
public interface ITenantAdminService
{
    Task<(bool Allowed, IReadOnlyList<TenantAdminUserResponse>? Users, string? Error)> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task<(bool Success, TenantAdminUserResponse? User, string? Error)> InviteOrAddUserAsync(InviteTenantUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> RemoveUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateUserRoleAsync(Guid userId, string role, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateUserModulesAsync(Guid userId, IReadOnlyList<string> moduleKeys, Guid currentUserId, CancellationToken cancellationToken = default);
}

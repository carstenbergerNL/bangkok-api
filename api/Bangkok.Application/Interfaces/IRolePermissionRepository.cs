using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}

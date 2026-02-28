using Bangkok.Application.Dto.Permissions;

namespace Bangkok.Application.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PermissionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PermissionResponse?> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<PermissionResponse?> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionResponse>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> AssignToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}

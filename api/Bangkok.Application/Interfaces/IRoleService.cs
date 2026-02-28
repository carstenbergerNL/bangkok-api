using Bangkok.Application.Dto.Roles;

namespace Bangkok.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleResponse?> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleResponse?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

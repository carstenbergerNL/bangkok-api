using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<Role>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

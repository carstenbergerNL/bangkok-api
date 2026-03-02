using Bangkok.Application.Interfaces;

namespace Bangkok.Infrastructure.Services;

public class UserPermissionChecker : IUserPermissionChecker
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IPermissionService _permissionService;

    public UserPermissionChecker(IUserRoleRepository userRoleRepository, IPermissionService permissionService)
    {
        _userRoleRepository = userRoleRepository;
        _permissionService = permissionService;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            return false;

        var roles = await _userRoleRepository.GetRolesByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        foreach (var role in roles)
        {
            var permissions = await _permissionService.GetByRoleIdAsync(role.Id, cancellationToken).ConfigureAwait(false);
            if (permissions.Any(p => string.Equals(p.Name, permissionName.Trim(), StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }
}

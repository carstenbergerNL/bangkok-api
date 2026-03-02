namespace Bangkok.Application.Interfaces;

/// <summary>
/// Checks whether a user has a given permission (via their roles). Used by services for permission enforcement.
/// </summary>
public interface IUserPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
}

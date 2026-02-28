using Bangkok.Application.Dto.Permissions;
using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleRepository roleRepository,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return permissions.Select(p => new PermissionResponse { Id = p.Id, Name = p.Name, Description = p.Description }).ToList();
    }

    public async Task<PermissionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return permission == null ? null : new PermissionResponse { Id = permission.Id, Name = permission.Name, Description = permission.Description };
    }

    public async Task<PermissionResponse?> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _permissionRepository.GetByNameAsync(request.Name.Trim(), cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return null;

        var permission = new Bangkok.Domain.Permission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        await _permissionRepository.CreateAsync(permission, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Permission created. PermissionId: {PermissionId}, Name: {Name}", permission.Id, permission.Name);
        return new PermissionResponse { Id = permission.Id, Name = permission.Name, Description = permission.Description };
    }

    public async Task<PermissionResponse?> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var existing = await _permissionRepository.GetByNameAsync(request.Name.Trim(), cancellationToken).ConfigureAwait(false);
            if (existing != null && existing.Id != id)
                return null;
            permission.Name = request.Name.Trim();
        }
        if (request.Description != null)
            permission.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _permissionRepository.UpdateAsync(permission, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Permission updated. PermissionId: {PermissionId}, Name: {Name}", permission.Id, permission.Name);
        return new PermissionResponse { Id = permission.Id, Name = permission.Name, Description = permission.Description };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return false;

        await _permissionRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Permission deleted. PermissionId: {PermissionId}, Name: {Name}", permission.Id, permission.Name);
        return true;
    }

    public async Task<IReadOnlyList<PermissionResponse>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return Array.Empty<PermissionResponse>();

        var permissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        return permissions.Select(p => new PermissionResponse { Id = p.Id, Name = p.Name, Description = p.Description }).ToList();
    }

    public async Task<bool> AssignToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken).ConfigureAwait(false);
        if (role == null || permission == null)
            return false;

        var exists = await _rolePermissionRepository.ExistsAsync(roleId, permissionId, cancellationToken).ConfigureAwait(false);
        if (exists)
            return true;

        await _rolePermissionRepository.AssignAsync(roleId, permissionId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Permission assigned to role. RoleId: {RoleId}, PermissionId: {PermissionId}, PermissionName: {PermissionName}", roleId, permissionId, permission.Name);
        return true;
    }

    public async Task<bool> RemoveFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken).ConfigureAwait(false);
        if (role == null || permission == null)
            return false;

        await _rolePermissionRepository.RemoveAsync(roleId, permissionId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Permission removed from role. RoleId: {RoleId}, PermissionId: {PermissionId}, PermissionName: {PermissionName}", roleId, permissionId, permission.Name);
        return true;
    }
}

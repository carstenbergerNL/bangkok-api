using Bangkok.Application.Dto.Roles;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IRoleRepository roleRepository, IUserRoleRepository userRoleRepository, IUserRepository userRepository, ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return roles.Select(r => new RoleResponse { Id = r.Id, Name = r.Name, Description = r.Description, CreatedAt = r.CreatedAt }).ToList();
    }

    public async Task<RoleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return role == null ? null : new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description, CreatedAt = role.CreatedAt };
    }

    public async Task<RoleResponse?> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _roleRepository.GetByNameAsync(request.Name.Trim(), cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return null;

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _roleRepository.CreateAsync(role, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Role created. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description, CreatedAt = role.CreatedAt };
    }

    public async Task<RoleResponse?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var existing = await _roleRepository.GetByNameAsync(request.Name.Trim(), cancellationToken).ConfigureAwait(false);
            if (existing != null && existing.Id != id)
                return null;
            role.Name = request.Name.Trim();
        }
        if (request.Description != null)
            role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _roleRepository.UpdateAsync(role, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Role updated. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description, CreatedAt = role.CreatedAt };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return false;

        await _roleRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Role deleted. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return true;
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (user == null || role == null)
            return false;

        var exists = await _userRoleRepository.ExistsAsync(userId, roleId, cancellationToken).ConfigureAwait(false);
        if (exists)
            return true;

        await _userRoleRepository.AssignAsync(userId, roleId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Role assigned. UserId: {UserId}, RoleId: {RoleId}, RoleName: {RoleName}", userId, roleId, role.Name);
        return true;
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (user == null || role == null)
            return false;

        var exists = await _userRoleRepository.ExistsAsync(userId, roleId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return true;

        await _userRoleRepository.RemoveAsync(userId, roleId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Role removed from user. UserId: {UserId}, RoleId: {RoleId}, RoleName: {RoleName}", userId, roleId, role.Name);
        return true;
    }
}

using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectMemberService : IProjectMemberService
{
    private const string AdminPermission = "ViewAdminSettings";
    private static readonly string[] ValidRoles = { "Owner", "Member", "Viewer" };

    private readonly IProjectMemberRepository _memberRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProjectMemberService> _logger;

    public ProjectMemberService(
        IProjectMemberRepository memberRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUserPermissionChecker permissionChecker,
        INotificationService notificationService,
        ILogger<ProjectMemberService> logger)
    {
        _memberRepository = memberRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _permissionChecker = permissionChecker;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<(bool Success, string? Role, string? Error)> GetCurrentUserRoleAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return (true, "Owner", null); // Admin treated as Owner for UI purposes

        var membership = await _memberRepository.GetByProjectAndUserAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false);
        if (membership == null)
            return (false, null, "You do not have access to this project.");
        return (true, membership.Role, null);
    }

    public async Task<(bool Success, IReadOnlyList<ProjectMemberResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        var isAdmin = await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        var membership = await _memberRepository.GetByProjectAndUserAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false);
        if (!isAdmin && membership == null)
        {
            _logger.LogWarning("User {UserId} attempted to list members of project {ProjectId} without membership.", currentUserId, projectId);
            return (false, null, "You do not have access to this project.");
        }

        var members = await _memberRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        var result = new List<ProjectMemberResponse>();
        foreach (var m in members)
        {
            var user = await _userRepository.GetByIdAsync(m.UserId, cancellationToken).ConfigureAwait(false);
            result.Add(new ProjectMemberResponse
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                UserId = m.UserId,
                UserDisplayName = user?.DisplayName ?? user?.Email ?? m.UserId.ToString(),
                UserEmail = user?.Email ?? "",
                Role = m.Role,
                CreatedAt = m.CreatedAt
            });
        }
        return (true, result, null);
    }

    public async Task<(bool Success, ProjectMemberResponse? Data, string? Error)> AddAsync(Guid projectId, CreateProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanManageMembersAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to add member to project {ProjectId} without permission.", currentUserId, projectId);
            return (false, null, "Only project owners or admins can add members.");
        }

        var role = NormalizeRole(request.Role);
        if (!ValidRoles.Contains(role))
            return (false, null, "Role must be Owner, Member, or Viewer.");

        var existing = await _memberRepository.GetByProjectAndUserAsync(projectId, request.UserId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return (false, null, "User is already a member of this project.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null || user.IsDeleted)
            return (false, null, "User not found.");

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = request.UserId,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        await _memberRepository.AddAsync(member, cancellationToken).ConfigureAwait(false);
        await _notificationService.CreateAsync(request.UserId, NotificationService.TypeMemberAddedToProject, "Added to project", $"You were added to project \"{project.Name}\" as {role}.", projectId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project member added. ProjectId: {ProjectId}, UserId: {UserId}, Role: {Role}, AddedByUserId: {CurrentUserId}", projectId, request.UserId, role, currentUserId);

        var response = new ProjectMemberResponse
        {
            Id = member.Id,
            ProjectId = member.ProjectId,
            UserId = member.UserId,
            UserDisplayName = user.DisplayName ?? user.Email ?? "",
            UserEmail = user.Email ?? "",
            Role = member.Role,
            CreatedAt = member.CreatedAt
        };
        return (true, response, null);
    }

    public async Task<(bool Success, string? Error)> UpdateRoleAsync(Guid projectId, Guid memberId, UpdateProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, "Project not found.");

        if (!await CanManageMembersAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update member role in project {ProjectId} without permission.", currentUserId, projectId);
            return (false, "Only project owners or admins can change roles.");
        }

        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null || member.ProjectId != projectId)
            return (false, "Member not found.");

        var newRole = NormalizeRole(request.Role);
        if (!ValidRoles.Contains(newRole))
            return (false, "Role must be Owner, Member, or Viewer.");

        if (member.UserId == currentUserId && member.Role == "Owner")
        {
            var ownerCount = await _memberRepository.CountOwnersAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (ownerCount <= 1)
                return (false, "Cannot change your role while you are the only owner. Add another owner first or leave the project.");
        }

        if (member.Role == "Owner" && newRole != "Owner")
        {
            var ownerCount = await _memberRepository.CountOwnersAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (ownerCount <= 1)
                return (false, "Cannot remove the last owner. Assign another owner first.");
        }

        member.Role = newRole;
        await _memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project member role updated. ProjectId: {ProjectId}, MemberId: {MemberId}, NewRole: {Role}, UpdatedByUserId: {CurrentUserId}", projectId, memberId, newRole, currentUserId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveAsync(Guid projectId, Guid memberId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, "Project not found.");

        if (!await CanManageMembersAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to remove member from project {ProjectId} without permission.", currentUserId, projectId);
            return (false, "Only project owners or admins can remove members.");
        }

        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
        if (member == null || member.ProjectId != projectId)
            return (false, "Member not found.");

        if (member.Role == "Owner")
        {
            var ownerCount = await _memberRepository.CountOwnersAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (ownerCount <= 1)
                return (false, "Cannot remove the last owner. Assign another owner first.");
        }

        await _memberRepository.DeleteAsync(memberId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project member removed. ProjectId: {ProjectId}, MemberId: {MemberId}, RemovedByUserId: {CurrentUserId}", projectId, memberId, currentUserId);
        return (true, null);
    }

    private async Task<bool> CanManageMembersAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m?.Role == "Owner";
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return "Member";
        return role.Trim();
    }
}

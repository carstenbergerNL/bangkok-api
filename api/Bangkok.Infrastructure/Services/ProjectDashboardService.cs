using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectDashboardService : IProjectDashboardService
{
    private const string AdminPermission = "ViewAdminSettings";

    private readonly IProjectDashboardRepository _dashboardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<ProjectDashboardService> _logger;

    public ProjectDashboardService(
        IProjectDashboardRepository dashboardRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<ProjectDashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<(bool Success, ProjectDashboardResponse? Data, string? Error)> GetDashboardAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanAccessProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to view dashboard for project {ProjectId} without access.", currentUserId, projectId);
            return (false, null, "You do not have access to this project.");
        }

        var data = await _dashboardRepository.GetDashboardAsync(projectId, cancellationToken).ConfigureAwait(false);
        return (true, data, null);
    }

    private async Task<bool> CanAccessProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m != null;
    }
}

using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectDashboardService
{
    Task<(bool Success, ProjectDashboardResponse? Data, string? Error)> GetDashboardAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
}

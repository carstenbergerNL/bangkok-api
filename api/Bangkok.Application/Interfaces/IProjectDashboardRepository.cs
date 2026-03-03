using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectDashboardRepository
{
    Task<ProjectDashboardResponse> GetDashboardAsync(Guid projectId, CancellationToken cancellationToken = default);
}

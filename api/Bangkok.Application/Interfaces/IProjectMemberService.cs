using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectMemberService
{
    Task<(bool Success, string? Role, string? Error)> GetCurrentUserRoleAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, IReadOnlyList<ProjectMemberResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectMemberResponse? Data, string? Error)> AddAsync(Guid projectId, CreateProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateRoleAsync(Guid projectId, Guid memberId, UpdateProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> RemoveAsync(Guid projectId, Guid memberId, Guid currentUserId, CancellationToken cancellationToken = default);
}

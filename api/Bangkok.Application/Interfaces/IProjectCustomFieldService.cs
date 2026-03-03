using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectCustomFieldService
{
    Task<(bool Success, IReadOnlyList<ProjectCustomFieldResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectCustomFieldResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateProjectCustomFieldRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectCustomFieldResponse? Data, string? Error)> UpdateAsync(Guid projectId, Guid fieldId, UpdateProjectCustomFieldRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid fieldId, Guid currentUserId, CancellationToken cancellationToken = default);
}

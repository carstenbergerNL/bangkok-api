using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectTemplateService
{
    Task<IReadOnlyList<ProjectTemplateResponse>> GetAllAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectTemplateResponse?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectTemplateResponse? Data, string? Error)> CreateAsync(CreateProjectTemplateRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectTemplateResponse? Data, string? Error)> UpdateAsync(Guid id, UpdateProjectTemplateRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}

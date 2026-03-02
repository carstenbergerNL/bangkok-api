using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public enum GetProjectResult
{
    Ok,
    NotFound,
    Forbidden
}

public interface IProjectService
{
    Task<(GetProjectResult Result, ProjectResponse? Data)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(CreateProjectResult Result, ProjectResponse? Data, string? ErrorMessage)> CreateAsync(CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(UpdateProjectResult Result, string? ErrorMessage)> UpdateAsync(Guid id, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeleteProjectResult> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}

public enum CreateProjectResult
{
    Success,
    Forbidden,
    ValidationError
}

public enum UpdateProjectResult
{
    Success,
    NotFound,
    Forbidden,
    ValidationError
}

public enum DeleteProjectResult
{
    Success,
    NotFound,
    Forbidden,
    HasTasks
}

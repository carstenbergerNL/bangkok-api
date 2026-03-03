using Bangkok.Application.Dto.Tasks;

namespace Bangkok.Application.Interfaces;

public enum GetTaskResult
{
    Ok,
    NotFound,
    Forbidden
}

public interface ITaskService
{
    Task<(GetTaskResult Result, TaskResponse? Data)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponse>> GetByProjectIdAsync(Guid projectId, Guid currentUserId, TaskFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task<(CreateTaskResult Result, TaskResponse? Data, string? ErrorMessage)> CreateAsync(CreateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(UpdateTaskResult Result, string? ErrorMessage)> UpdateAsync(Guid id, UpdateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeleteTaskResult> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}

public enum CreateTaskResult
{
    Success,
    Forbidden,
    ProjectNotFound,
    ValidationError
}

public enum UpdateTaskResult
{
    Success,
    NotFound,
    Forbidden,
    ValidationError,
    AssignForbidden
}

public enum DeleteTaskResult
{
    Success,
    NotFound,
    Forbidden
}

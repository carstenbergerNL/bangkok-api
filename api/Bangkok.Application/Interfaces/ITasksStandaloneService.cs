using Bangkok.Application.Dto.TasksStandalone;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Standalone tasks module service. Tenant-scoped; enforces permissions and subscription limits.
/// </summary>
public interface ITasksStandaloneService
{
    Task<(bool Success, IReadOnlyList<TasksStandaloneResponse>? Data, string? Error)> GetListAsync(Guid currentUserId, TasksStandaloneFilterRequest? filter, bool myTasksOnly, CancellationToken cancellationToken = default);
    Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> CreateAsync(CreateTasksStandaloneRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> UpdateAsync(Guid id, UpdateTasksStandaloneRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> SetStatusAsync(Guid id, string status, Guid currentUserId, CancellationToken cancellationToken = default);
}

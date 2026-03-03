using Bangkok.Application.Dto.Tasks;

namespace Bangkok.Application.Interfaces;

public interface ITaskTimeLogService
{
    Task<IReadOnlyList<TaskTimeLogResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TaskTimeLogResponse? Data, string? Error)> CreateAsync(Guid taskId, CreateTaskTimeLogRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid timeLogId, Guid currentUserId, CancellationToken cancellationToken = default);
}

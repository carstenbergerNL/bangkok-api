using Bangkok.Application.Dto.Tasks;

namespace Bangkok.Application.Interfaces;

public interface ITaskCommentService
{
    Task<IReadOnlyList<TaskCommentResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TaskCommentResponse? Data, string? Error)> CreateAsync(Guid taskId, CreateTaskCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(Guid commentId, UpdateTaskCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}

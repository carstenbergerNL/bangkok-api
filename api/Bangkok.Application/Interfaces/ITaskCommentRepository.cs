using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskCommentRepository
{
    Task<IReadOnlyList<TaskComment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TaskComment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskComment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

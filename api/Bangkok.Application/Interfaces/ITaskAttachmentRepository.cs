using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskAttachmentRepository
{
    Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

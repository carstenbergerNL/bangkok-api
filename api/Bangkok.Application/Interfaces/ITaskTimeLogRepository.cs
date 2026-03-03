using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskTimeLogRepository
{
    Task<TaskTimeLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskTimeLog>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalLoggedHoursByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetTotalLoggedHoursByTaskIdsAsync(IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TaskTimeLog log, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

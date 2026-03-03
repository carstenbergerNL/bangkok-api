using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskActivityRepository
{
    Task<IReadOnlyList<TaskActivity>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TaskActivity activity, CancellationToken cancellationToken = default);
}

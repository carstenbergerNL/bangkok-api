namespace Bangkok.Application.Interfaces;

public interface ITaskLabelRepository
{
    Task<IReadOnlyList<Guid>> GetLabelIdsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task SetForTaskAsync(Guid taskId, IReadOnlyList<Guid> labelIds, CancellationToken cancellationToken = default);
}

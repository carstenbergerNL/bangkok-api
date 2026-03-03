using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskCustomFieldValueRepository
{
    Task<IReadOnlyList<TaskCustomFieldValue>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, IReadOnlyList<TaskCustomFieldValue>>> GetByTaskIdsAsync(IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default);
    Task SetForTaskAsync(Guid taskId, IReadOnlyList<(Guid FieldId, string? Value)> values, CancellationToken cancellationToken = default);
}

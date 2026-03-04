using Bangkok.Application.Dto.TasksStandalone;
using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

/// <summary>
/// Repository for standalone tasks (tenant-scoped). All queries filter by TenantId.
/// </summary>
public interface ITasksStandaloneRepository
{
    Task<TasksStandalone?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TasksStandalone>> GetListAsync(Guid tenantId, TasksStandaloneFilterRequest? filter, Guid? assignedToUserIdOnly, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TasksStandalone entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TasksStandalone entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}

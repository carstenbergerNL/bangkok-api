using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ITaskRepository
{
    System.Threading.Tasks.Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<IReadOnlyList<ProjectTask>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<Guid> CreateAsync(ProjectTask task, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task UpdateAsync(ProjectTask task, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

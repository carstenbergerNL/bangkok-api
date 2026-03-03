using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProjectCustomFieldRepository
{
    Task<ProjectCustomField?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectCustomField>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ProjectCustomField field, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectCustomField field, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

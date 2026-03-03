using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProjectTemplateRepository
{
    Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ProjectTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

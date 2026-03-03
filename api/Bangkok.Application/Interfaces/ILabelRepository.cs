using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface ILabelRepository
{
    Task<Label?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Label>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Label label, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

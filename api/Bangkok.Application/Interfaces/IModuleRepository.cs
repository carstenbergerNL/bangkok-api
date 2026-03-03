using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default);
}

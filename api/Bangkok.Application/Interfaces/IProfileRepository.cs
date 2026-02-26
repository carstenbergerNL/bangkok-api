using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProfileRepository
{
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Profile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

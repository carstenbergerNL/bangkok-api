using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Plan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken cancellationToken = default);
}

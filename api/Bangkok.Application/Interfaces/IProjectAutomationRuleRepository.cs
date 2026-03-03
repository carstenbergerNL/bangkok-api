using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProjectAutomationRuleRepository
{
    Task<IReadOnlyList<ProjectAutomationRule>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectAutomationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ProjectAutomationRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface IProjectAutomationRuleService
{
    Task<(bool Success, IReadOnlyList<ProjectAutomationRuleResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, ProjectAutomationRuleResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateProjectAutomationRuleRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid ruleId, Guid currentUserId, CancellationToken cancellationToken = default);
}

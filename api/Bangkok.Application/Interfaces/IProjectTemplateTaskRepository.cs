using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProjectTemplateTaskRepository
{
    Task<IReadOnlyList<ProjectTemplateTask>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task CreateAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default);
    Task DeleteByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
}

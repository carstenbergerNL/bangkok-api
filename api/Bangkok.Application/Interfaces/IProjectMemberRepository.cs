using Bangkok.Domain;

namespace Bangkok.Application.Interfaces;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetProjectIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountOwnersAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectMember member, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}

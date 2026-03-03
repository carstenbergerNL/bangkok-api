using Bangkok.Application.Dto.Projects;

namespace Bangkok.Application.Interfaces;

public interface ILabelService
{
    Task<(bool Success, IReadOnlyList<LabelResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, LabelResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);
}

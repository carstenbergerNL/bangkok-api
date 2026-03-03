using Bangkok.Application.Dto.Tasks;

namespace Bangkok.Application.Interfaces;

public interface ITaskActivityService
{
    Task<IReadOnlyList<TaskActivityResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);
}

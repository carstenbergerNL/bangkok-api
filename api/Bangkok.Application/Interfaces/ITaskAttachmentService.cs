using Bangkok.Application.Dto.Tasks;

namespace Bangkok.Application.Interfaces;

public interface ITaskAttachmentService
{
    Task<IReadOnlyList<TaskAttachmentResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, TaskAttachmentResponse? Data, string? Error)> UploadAsync(Guid taskId, string fileName, Stream fileStream, string contentType, int fileSize, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, Stream? Content, string? FileName, string? ContentType, string? Error)> GetDownloadStreamAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default);
}

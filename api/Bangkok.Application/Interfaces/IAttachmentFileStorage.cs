namespace Bangkok.Application.Interfaces;

/// <summary>
/// Local file storage for task attachments. Saves under uploads/tasks/{taskId}/.
/// </summary>
public interface IAttachmentFileStorage
{
    /// <summary>Save file; returns relative path to store in DB (e.g. uploads/tasks/{taskId}/filename).</summary>
    Task<string> SaveAsync(Guid taskId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Delete file by relative path. No-op if file does not exist.</summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>Open read stream for download. Returns null if file does not exist.</summary>
    Task<Stream?> GetStreamAsync(string relativePath, CancellationToken cancellationToken = default);
}

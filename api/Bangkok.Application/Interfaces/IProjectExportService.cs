namespace Bangkok.Application.Interfaces;

public interface IProjectExportService
{
    /// <summary>
    /// Exports project tasks to CSV. Returns (null, error) if project not found or access denied.
    /// </summary>
    Task<(byte[]? CsvBytes, string? Error)> ExportToCsvAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
}

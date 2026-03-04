using System.IO;
using Bangkok.Application.Configuration;
using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bangkok.Infrastructure.Services;

public class TaskAttachmentService : ITaskAttachmentService
{
    private const string AdminPermission = "ViewAdminSettings";

    private readonly ITaskAttachmentRepository _attachmentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly ITenantUsageRepository _usageRepository;
    private readonly IAttachmentFileStorage _fileStorage;
    private readonly IOptions<AttachmentSettings> _options;
    private readonly ILogger<TaskAttachmentService> _logger;

    public TaskAttachmentService(
        ITaskAttachmentRepository attachmentRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserPermissionChecker permissionChecker,
        ISubscriptionLimitService subscriptionLimitService,
        ITenantUsageRepository usageRepository,
        IAttachmentFileStorage fileStorage,
        IOptions<AttachmentSettings> options,
        ILogger<TaskAttachmentService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _permissionChecker = permissionChecker;
        _subscriptionLimitService = subscriptionLimitService;
        _usageRepository = usageRepository;
        _fileStorage = fileStorage;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskAttachmentResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessTaskAsync(taskId, currentUserId, cancellationToken).ConfigureAwait(false))
            return Array.Empty<TaskAttachmentResponse>();
        var list = await _attachmentRepository.GetByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        return list.Select(Map).ToList();
    }

    public async Task<(bool Success, TaskAttachmentResponse? Data, string? Error)> UploadAsync(Guid taskId, string fileName, Stream fileStream, string contentType, int fileSize, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessTaskAsync(taskId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "Task not found or access denied.");
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (false, null, "Task not found.");
        var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");
        var settings = _options.Value;
        if (fileSize <= 0 || fileSize > settings.MaxFileSizeBytes)
            return (false, null, $"File size must be between 1 and {settings.MaxFileSizeBytes / (1024 * 1024)} MB.");
        var fileSizeMb = fileSize / (1024m * 1024m);
        var (canAddStorage, storageLimitMsg) = await _subscriptionLimitService.CanAddStorageAsync(project.TenantId, fileSizeMb, cancellationToken).ConfigureAwait(false);
        if (!canAddStorage)
            return (false, null, storageLimitMsg ?? "Storage limit reached. Upgrade your plan for more storage.");
        if (!IsContentTypeAllowed(contentType, settings))
            return (false, null, "File type is not allowed.");
        var sanitizedContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        string relativePath;
        try
        {
            relativePath = await _fileStorage.SaveAsync(taskId, fileName, fileStream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save attachment file for task {TaskId}", taskId);
            return (false, null, "Failed to save file.");
        }
        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FileName = Path.GetFileName(fileName),
            FilePath = relativePath,
            FileSize = fileSize,
            ContentType = sanitizedContentType,
            UploadedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _attachmentRepository.CreateAsync(attachment, cancellationToken).ConfigureAwait(false);
        await _usageRepository.AddStorageMbAsync(project.TenantId, fileSizeMb, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Attachment uploaded. TaskId: {TaskId}, AttachmentId: {Id}, FileName: {FileName}", taskId, attachment.Id, attachment.FileName);
        return (true, Map(attachment), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken).ConfigureAwait(false);
        if (attachment == null)
            return (false, "Attachment not found.");
        if (!await CanAccessTaskAsync(attachment.TaskId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "Access denied.");
        var isAdmin = await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        var isUploader = attachment.UploadedByUserId == currentUserId;
        if (!isAdmin && !isUploader)
            return (false, "Only the uploader or an admin can delete this attachment.");
        var task = await _taskRepository.GetByIdAsync(attachment.TaskId, cancellationToken).ConfigureAwait(false);
        var project = task != null ? await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false) : null;
        await _fileStorage.DeleteAsync(attachment.FilePath, cancellationToken).ConfigureAwait(false);
        await _attachmentRepository.DeleteAsync(attachmentId, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            var fileSizeMb = attachment.FileSize / (1024m * 1024m);
            await _usageRepository.RemoveStorageMbAsync(project.TenantId, fileSizeMb, cancellationToken).ConfigureAwait(false);
        }
        _logger.LogInformation("Attachment deleted. Id: {Id}, DeletedBy: {UserId}", attachmentId, currentUserId);
        return (true, null);
    }

    public async Task<(bool Success, Stream? Content, string? FileName, string? ContentType, string? Error)> GetDownloadStreamAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken).ConfigureAwait(false);
        if (attachment == null)
            return (false, null, null, null, "Attachment not found.");
        if (!await CanAccessTaskAsync(attachment.TaskId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, null, null, "Access denied.");
        var stream = await _fileStorage.GetStreamAsync(attachment.FilePath, cancellationToken).ConfigureAwait(false);
        if (stream == null)
            return (false, null, null, null, "File not found on server.");
        return (true, stream, attachment.FileName, attachment.ContentType, null);
    }

    private async Task<bool> CanAccessTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null) return false;
        var member = await _memberRepository.GetByProjectAndUserAsync(task.ProjectId, userId, cancellationToken).ConfigureAwait(false);
        return member != null;
    }

    private static bool IsContentTypeAllowed(string contentType, AttachmentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var ct = contentType.Trim().ToLowerInvariant();
        if (settings.AllowedContentTypes == null || settings.AllowedContentTypes.Length == 0) return true;
        foreach (var allowed in settings.AllowedContentTypes)
        {
            var a = allowed?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(a)) continue;
            if (a.EndsWith("*", StringComparison.Ordinal))
            {
                var prefix = a[..^1];
                if (ct.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            else if (ct.Equals(a, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static TaskAttachmentResponse Map(TaskAttachment a) => new()
    {
        Id = a.Id,
        TaskId = a.TaskId,
        FileName = a.FileName,
        FileSize = a.FileSize,
        ContentType = a.ContentType,
        UploadedByUserId = a.UploadedByUserId,
        CreatedAt = a.CreatedAt
    };
}

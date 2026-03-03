using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TaskCommentService : ITaskCommentService
{
    private const string PermissionComment = "Task.Comment";
    private const string PermissionView = "Task.View";
    private const string PermissionDelete = "Task.Delete";

    private readonly ITaskCommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<TaskCommentService> _logger;

    public TaskCommentService(
        ITaskCommentRepository commentRepository,
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<TaskCommentService> logger)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskCommentResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
            return Array.Empty<TaskCommentResponse>();

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return Array.Empty<TaskCommentResponse>();

        var comments = await _commentRepository.GetByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var userMap = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await _userRepository.GetByIdAsync(uid, cancellationToken).ConfigureAwait(false);
            userMap[uid] = user?.DisplayName ?? user?.Email ?? uid.ToString();
        }

        return comments.Select(c => new TaskCommentResponse
        {
            Id = c.Id,
            TaskId = c.TaskId,
            UserId = c.UserId,
            UserDisplayName = userMap.GetValueOrDefault(c.UserId),
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    public async Task<(bool Success, TaskCommentResponse? Data, string? Error)> CreateAsync(Guid taskId, CreateTaskCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionComment, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to add comment without Task.Comment", currentUserId);
            return (false, null, "You do not have permission to comment on tasks.");
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (false, null, "Task not found.");

        var content = request?.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            return (false, null, "Content is required.");
        if (content.Length > 2000)
            return (false, null, "Content must be at most 2000 characters.");

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = currentUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.CreateAsync(comment, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Comment created. CommentId: {CommentId}, TaskId: {TaskId}, UserId: {UserId}", comment.Id, taskId, currentUserId);

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        return (true, new TaskCommentResponse
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserDisplayName = user?.DisplayName ?? user?.Email,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = null
        }, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Guid commentId, UpdateTaskCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionComment, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update comment without Task.Comment", currentUserId);
            return (false, "You do not have permission to edit comments.");
        }

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment == null)
            return (false, "Comment not found.");
        if (comment.UserId != currentUserId)
            return (false, "You can only edit your own comment.");

        var content = request?.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            return (false, "Content is required.");
        if (content.Length > 2000)
            return (false, "Content must be at most 2000 characters.");

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _commentRepository.UpdateAsync(comment, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionComment, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete comment without Task.Comment", currentUserId);
            return (false, "You do not have permission to delete comments.");
        }

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment == null)
            return (false, "Comment not found.");

        var canDeleteAny = await _permissionChecker.HasPermissionAsync(currentUserId, PermissionDelete, cancellationToken).ConfigureAwait(false);
        if (comment.UserId != currentUserId && !canDeleteAny)
            return (false, "You can only delete your own comment.");

        await _commentRepository.DeleteAsync(commentId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Comment deleted. CommentId: {CommentId}, DeletedByUserId: {UserId}", commentId, currentUserId);
        return (true, null);
    }
}

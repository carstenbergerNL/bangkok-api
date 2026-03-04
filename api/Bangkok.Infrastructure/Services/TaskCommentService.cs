using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TaskCommentService : ITaskCommentService
{
    private const string AdminPermission = "ViewAdminSettings";

    private readonly ITaskCommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<TaskCommentService> _logger;

    public TaskCommentService(
        ITaskCommentRepository commentRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IUserPermissionChecker permissionChecker,
        ILogger<TaskCommentService> logger)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskCommentResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return Array.Empty<TaskCommentResponse>();

        if (!await CanAccessProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
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
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (false, null, "Task not found.");

        var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            var (canWrite, writeError) = await CanWriteInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
            if (!canWrite)
            {
                _logger.LogWarning("User {UserId} attempted to add comment in project {ProjectId}: {Reason}", currentUserId, task.ProjectId, writeError);
                return (false, null, writeError);
            }
        }

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "You must be a project member (Member or Owner) to comment.");

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

        await NotifyMentionsAsync(content!, taskId, task.Title, currentUserId, cancellationToken).ConfigureAwait(false);

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
        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment == null)
            return (false, "Comment not found.");
        var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken).ConfigureAwait(false);
        if (task != null)
        {
            var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
            if (project != null)
            {
                var (canWrite, writeError) = await CanWriteInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
                if (!canWrite)
                {
                    _logger.LogWarning("User {UserId} attempted to update comment in project {ProjectId}: {Reason}", currentUserId, task.ProjectId, writeError);
                    return (false, writeError);
                }
            }
            if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
                return (false, "You must be a project member to edit comments.");
        }
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
        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment == null)
            return (false, "Comment not found.");
        var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken).ConfigureAwait(false);
        if (task != null)
        {
            var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
            if (project != null)
            {
                var (canWrite, writeError) = await CanWriteInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
                if (!canWrite)
                {
                    _logger.LogWarning("User {UserId} attempted to delete comment in project {ProjectId}: {Reason}", currentUserId, task.ProjectId, writeError);
                    return (false, writeError);
                }
            }
            if (!await CanAccessProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
                return (false, "You do not have access to this project.");
        }

        if (task == null)
            return (false, "Task not found.");

        var canDeleteAny = await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false);
        if (comment.UserId != currentUserId && !canDeleteAny)
            return (false, "You can only delete your own comment.");

        await _commentRepository.DeleteAsync(commentId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Comment deleted. CommentId: {CommentId}, DeletedByUserId: {UserId}", commentId, currentUserId);
        return (true, null);
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CanWriteInProjectAsync(Project project, Guid userId, CancellationToken cancellationToken)
    {
        if (string.Equals(project.Status, "Archived", StringComparison.OrdinalIgnoreCase))
            return (false, "Project is archived; tasks are read-only.");
        if (string.Equals(project.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            var isAdmin = await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false);
            if (!isAdmin)
                return (false, "Tasks are locked for completed projects. Only admins can modify tasks.");
        }
        return (true, null);
    }

    private async Task<bool> CanAccessProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m != null;
    }

    private async Task<bool> CanEditProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m != null && (m.Role == "Member" || m.Role == "Owner");
    }

    private async Task NotifyMentionsAsync(string content, Guid taskId, string taskTitle, Guid commentAuthorId, CancellationToken cancellationToken)
    {
        var tokens = System.Text.RegularExpressions.Regex.Matches(content, @"@(\S+)")
            .Select(m => m.Groups[1].Value.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tokens.Count == 0) return;

        var notified = new HashSet<Guid>();
        foreach (var token in tokens)
        {
            User? user = token.Contains('@', StringComparison.Ordinal)
                ? await _userRepository.GetByEmailAsync(token, cancellationToken).ConfigureAwait(false)
                : await _userRepository.GetByDisplayNameAsync(token, cancellationToken).ConfigureAwait(false);
            if (user == null || user.Id == commentAuthorId || notified.Contains(user.Id))
                continue;
            notified.Add(user.Id);
            await _notificationService.CreateAsync(user.Id, NotificationService.TypeMention, "You were mentioned", $"You were mentioned in a comment on task: {taskTitle}", taskId, cancellationToken).ConfigureAwait(false);
        }
    }
}

using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TaskTimeLogService : ITaskTimeLogService
{
    private const string PermissionView = "Task.View";
    private const string PermissionEdit = "Task.Edit";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly ITaskTimeLogRepository _timeLogRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<TaskTimeLogService> _logger;

    public TaskTimeLogService(
        ITaskTimeLogRepository timeLogRepository,
        ITaskRepository taskRepository,
        IProjectMemberRepository memberRepository,
        IUserRepository userRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<TaskTimeLogService> logger)
    {
        _timeLogRepository = timeLogRepository;
        _taskRepository = taskRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskTimeLogResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
            return Array.Empty<TaskTimeLogResponse>();

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null) return Array.Empty<TaskTimeLogResponse>();

        if (!await CanAccessProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return Array.Empty<TaskTimeLogResponse>();

        var logs = await _timeLogRepository.GetByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        var userIds = logs.Select(l => l.UserId).Distinct().ToList();
        var userMap = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await _userRepository.GetByIdAsync(uid, cancellationToken).ConfigureAwait(false);
            userMap[uid] = user?.DisplayName ?? user?.Email ?? uid.ToString();
        }

        return logs.Select(l => new TaskTimeLogResponse
        {
            Id = l.Id,
            TaskId = l.TaskId,
            UserId = l.UserId,
            UserDisplayName = userMap.GetValueOrDefault(l.UserId),
            Hours = l.Hours,
            Description = l.Description,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    public async Task<(bool Success, TaskTimeLogResponse? Data, string? Error)> CreateAsync(Guid taskId, CreateTaskTimeLogRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to log time without Task.Edit", currentUserId);
            return (false, null, "You do not have permission to log time on tasks.");
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (false, null, "Task not found.");

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "You must be a project member (Member or Owner) to log time.");

        if (request.Hours < 0.01m || request.Hours > 999.99m)
            return (false, null, "Hours must be between 0.01 and 999.99.");

        var log = new TaskTimeLog
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = currentUserId,
            Hours = request.Hours,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _timeLogRepository.CreateAsync(log, cancellationToken).ConfigureAwait(false);

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        return (true, new TaskTimeLogResponse
        {
            Id = log.Id,
            TaskId = log.TaskId,
            UserId = log.UserId,
            UserDisplayName = user?.DisplayName ?? user?.Email,
            Hours = log.Hours,
            Description = log.Description,
            CreatedAt = log.CreatedAt
        }, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid timeLogId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete time log without Task.Edit", currentUserId);
            return (false, "You do not have permission to delete time logs.");
        }

        var log = await _timeLogRepository.GetByIdAsync(timeLogId, cancellationToken).ConfigureAwait(false);
        if (log == null)
            return (false, "Time log not found.");

        var task = await _taskRepository.GetByIdAsync(log.TaskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (false, "Task not found.");

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "You do not have permission to delete this time log.");

        await _timeLogRepository.DeleteAsync(timeLogId, cancellationToken).ConfigureAwait(false);
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
}

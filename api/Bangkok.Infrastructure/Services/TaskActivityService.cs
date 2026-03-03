using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TaskActivityService : ITaskActivityService
{
    private const string PermissionViewActivity = "Task.ViewActivity";

    private readonly ITaskActivityRepository _activityRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionChecker _permissionChecker;

    public TaskActivityService(
        ITaskActivityRepository activityRepository,
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IUserPermissionChecker permissionChecker)
    {
        _activityRepository = activityRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _permissionChecker = permissionChecker;
    }

    public async Task<IReadOnlyList<TaskActivityResponse>> GetByTaskIdAsync(Guid taskId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionViewActivity, cancellationToken).ConfigureAwait(false))
            return Array.Empty<TaskActivityResponse>();

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return Array.Empty<TaskActivityResponse>();

        var activities = await _activityRepository.GetByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        var userIds = activities.Select(a => a.UserId).Distinct().ToList();
        var userMap = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await _userRepository.GetByIdAsync(uid, cancellationToken).ConfigureAwait(false);
            userMap[uid] = user?.DisplayName ?? user?.Email ?? uid.ToString();
        }

        return activities.Select(a => new TaskActivityResponse
        {
            Id = a.Id,
            TaskId = a.TaskId,
            UserId = a.UserId,
            UserDisplayName = userMap.GetValueOrDefault(a.UserId),
            Action = a.Action,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            CreatedAt = a.CreatedAt
        }).ToList();
    }
}

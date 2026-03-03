using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Dto.Tasks;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TaskService : ITaskService
{
    private const string PermissionView = "Task.View";
    private const string PermissionCreate = "Task.Create";
    private const string PermissionEdit = "Task.Edit";
    private const string PermissionDelete = "Task.Delete";
    private const string PermissionAssign = "Task.Assign";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly ITaskActivityRepository _activityRepository;
    private readonly ITaskLabelRepository _taskLabelRepository;
    private readonly ITaskCustomFieldValueRepository _taskCustomFieldValueRepository;
    private readonly IProjectCustomFieldRepository _projectCustomFieldRepository;
    private readonly ITaskAttachmentRepository _attachmentRepository;
    private readonly IAttachmentFileStorage _attachmentFileStorage;
    private readonly ILabelRepository _labelRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository taskRepository, IProjectRepository projectRepository, IProjectMemberRepository memberRepository, ITaskActivityRepository activityRepository, ITaskLabelRepository taskLabelRepository, ITaskCustomFieldValueRepository taskCustomFieldValueRepository, IProjectCustomFieldRepository projectCustomFieldRepository, ITaskAttachmentRepository attachmentRepository, IAttachmentFileStorage attachmentFileStorage, ILabelRepository labelRepository, IUserPermissionChecker permissionChecker, INotificationService notificationService, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _activityRepository = activityRepository;
        _taskLabelRepository = taskLabelRepository;
        _taskCustomFieldValueRepository = taskCustomFieldValueRepository;
        _projectCustomFieldRepository = projectCustomFieldRepository;
        _attachmentRepository = attachmentRepository;
        _attachmentFileStorage = attachmentFileStorage;
        _labelRepository = labelRepository;
        _permissionChecker = permissionChecker;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<(GetTaskResult Result, TaskResponse? Data)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to get task {TaskId} without Task.View", currentUserId, id);
            return (GetTaskResult.Forbidden, null);
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (GetTaskResult.NotFound, null);

        if (!await CanAccessProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to get task {TaskId} without project membership.", currentUserId, id);
            return (GetTaskResult.Forbidden, null);
        }

        var labels = await GetLabelsForTaskAsync(task.Id, task.ProjectId, cancellationToken).ConfigureAwait(false);
        var customValues = await _taskCustomFieldValueRepository.GetByTaskIdAsync(task.Id, cancellationToken).ConfigureAwait(false);
        var customValueResponses = customValues.Select(v => new TaskCustomFieldValueResponse { FieldId = v.FieldId, Value = v.Value }).ToList();
        return (GetTaskResult.Ok, MapToResponse(task, labels, customValueResponses));
    }

    public async Task<IReadOnlyList<TaskResponse>> GetByProjectIdAsync(Guid projectId, Guid currentUserId, TaskFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list tasks without Task.View", currentUserId);
            return Array.Empty<TaskResponse>();
        }

        if (!await CanAccessProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list tasks for project {ProjectId} without membership.", currentUserId, projectId);
            return Array.Empty<TaskResponse>();
        }

        var tasks = await _taskRepository.GetByProjectIdAsync(projectId, filter, cancellationToken).ConfigureAwait(false);
        var taskIds = tasks.Select(t => t.Id).ToList();
        var labelsByTask = await GetLabelsByTaskIdsAsync(taskIds, projectId, cancellationToken).ConfigureAwait(false);
        return tasks.Select(t => MapToResponse(t, labelsByTask.GetValueOrDefault(t.Id) ?? Array.Empty<LabelResponse>())).ToList();
    }

    public async Task<(CreateTaskResult Result, TaskResponse? Data, string? ErrorMessage)> CreateAsync(CreateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionCreate, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create task without Task.Create", currentUserId);
            return (CreateTaskResult.Forbidden, null, "You do not have permission to create tasks.");
        }

        if (request == null)
            return (CreateTaskResult.ValidationError, null, "Request is required.");
        if (request.ProjectId == Guid.Empty)
            return (CreateTaskResult.ValidationError, null, "ProjectId is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            return (CreateTaskResult.ValidationError, null, "Title is required.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (CreateTaskResult.ProjectNotFound, null, "Project not found.");

        var (canWrite, writeError) = await CanWriteTasksInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
        if (!canWrite)
        {
            _logger.LogWarning("User {UserId} attempted to create task in project {ProjectId}: {Reason}", currentUserId, request.ProjectId, writeError);
            return (CreateTaskResult.Forbidden, null, writeError);
        }

        if (!await CanEditProjectAsync(request.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create task in project {ProjectId} without Member/Owner role.", currentUserId, request.ProjectId);
            return (CreateTaskResult.Forbidden, null, "You must be a project member (Member or Owner) to create tasks.");
        }

        var isRecurring = request.IsRecurring && !string.IsNullOrWhiteSpace(request.RecurrencePattern) && request.RecurrenceInterval.HasValue && request.RecurrenceInterval.Value >= 1;
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ToDo" : request.Status.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority.Trim(),
            AssignedToUserId = request.AssignedToUserId == Guid.Empty ? null : request.AssignedToUserId,
            DueDate = request.DueDate?.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc) : request.DueDate,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            EstimatedHours = request.EstimatedHours,
            IsRecurring = isRecurring,
            RecurrencePattern = isRecurring ? request.RecurrencePattern!.Trim() : null,
            RecurrenceInterval = isRecurring ? request.RecurrenceInterval : null,
            RecurrenceEndDate = isRecurring && request.RecurrenceEndDate.HasValue ? (request.RecurrenceEndDate.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.RecurrenceEndDate.Value, DateTimeKind.Utc) : request.RecurrenceEndDate.Value) : null,
            RecurrenceSourceTaskId = null
        };

        await _taskRepository.CreateAsync(task, cancellationToken).ConfigureAwait(false);

        var labelIds = request.LabelIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (labelIds.Count > 0)
        {
            var projectLabels = await _labelRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
            var validIds = projectLabels.Select(l => l.Id).ToHashSet();
            var toSet = labelIds.Where(id => validIds.Contains(id)).ToList();
            await _taskLabelRepository.SetForTaskAsync(task.Id, toSet, cancellationToken).ConfigureAwait(false);
        }

        await LogActivityAsync(task.Id, currentUserId, "TaskCreated", null, task.Title, cancellationToken).ConfigureAwait(false);
        if (task.AssignedToUserId.HasValue && task.AssignedToUserId.Value != currentUserId && task.AssignedToUserId.Value != Guid.Empty)
            await _notificationService.CreateAsync(task.AssignedToUserId.Value, NotificationService.TypeTaskAssigned, "Task assigned", $"You were assigned to: {task.Title}", task.Id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Task created. TaskId: {TaskId}, ProjectId: {ProjectId}, Title: {Title}, CreatedByUserId: {CreatedByUserId}", task.Id, task.ProjectId, task.Title, currentUserId);

        var labels = await GetLabelsForTaskAsync(task.Id, task.ProjectId, cancellationToken).ConfigureAwait(false);
        var customValueResponses = await SaveTaskCustomFieldValuesAsync(task.Id, task.ProjectId, request.CustomFieldValues, cancellationToken).ConfigureAwait(false);
        return (CreateTaskResult.Success, MapToResponse(task, labels, customValueResponses), null);
    }

    public async Task<(UpdateTaskResult Result, string? ErrorMessage)> UpdateAsync(Guid id, UpdateTaskRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update task {TaskId} without Task.Edit", currentUserId, id);
            return (UpdateTaskResult.Forbidden, "You do not have permission to edit tasks.");
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return (UpdateTaskResult.NotFound, null);

        var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            var (canWrite, writeError) = await CanWriteTasksInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
            if (!canWrite)
            {
                _logger.LogWarning("User {UserId} attempted to update task {TaskId}: {Reason}", currentUserId, id, writeError);
                return (UpdateTaskResult.Forbidden, writeError);
            }
        }

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update task {TaskId} without project Member/Owner role.", currentUserId, id);
            return (UpdateTaskResult.Forbidden, "You must be a project member (Member or Owner) to edit tasks.");
        }

        var oldStatus = task.Status;
        var oldPriority = task.Priority;
        var oldAssignedTo = task.AssignedToUserId;
        var oldDueDate = task.DueDate;

        if (request.AssignedToUserId.HasValue && request.AssignedToUserId.Value != Guid.Empty)
        {
            if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionAssign, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning("User {UserId} attempted to assign task {TaskId} without Task.Assign", currentUserId, id);
                return (UpdateTaskResult.AssignForbidden, "You do not have permission to assign tasks.");
            }
        }

        if (request.Title != null)
            task.Title = request.Title.Trim();
        if (request.Description != null)
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.Status != null)
            task.Status = request.Status.Trim();
        if (request.Priority != null)
            task.Priority = request.Priority.Trim();
        if (request.AssignedToUserId != null)
            task.AssignedToUserId = request.AssignedToUserId == Guid.Empty ? null : request.AssignedToUserId;
        if (request.DueDate.HasValue)
            task.DueDate = request.DueDate.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc) : request.DueDate.Value;
        if (request.EstimatedHours.HasValue)
            task.EstimatedHours = request.EstimatedHours.Value;
        if (request.IsRecurring.HasValue)
        {
            var enableRecurring = request.IsRecurring.Value && !string.IsNullOrWhiteSpace(request.RecurrencePattern) && request.RecurrenceInterval.HasValue && request.RecurrenceInterval.Value >= 1;
            task.IsRecurring = enableRecurring;
            task.RecurrencePattern = enableRecurring ? (request.RecurrencePattern ?? task.RecurrencePattern)?.Trim() : null;
            task.RecurrenceInterval = enableRecurring ? request.RecurrenceInterval : null;
            task.RecurrenceEndDate = enableRecurring && request.RecurrenceEndDate.HasValue ? (request.RecurrenceEndDate.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.RecurrenceEndDate.Value, DateTimeKind.Utc) : request.RecurrenceEndDate.Value) : (enableRecurring ? task.RecurrenceEndDate : null);
            if (!enableRecurring) task.RecurrenceSourceTaskId = null;
        }
        else if (request.RecurrencePattern != null || request.RecurrenceInterval.HasValue)
        {
            var enableRecurring = task.IsRecurring && !string.IsNullOrWhiteSpace(request.RecurrencePattern ?? task.RecurrencePattern) && (request.RecurrenceInterval ?? task.RecurrenceInterval).GetValueOrDefault(1) >= 1;
            task.RecurrencePattern = enableRecurring ? (request.RecurrencePattern ?? task.RecurrencePattern)?.Trim() : null;
            task.RecurrenceInterval = enableRecurring ? (request.RecurrenceInterval ?? task.RecurrenceInterval) : null;
            task.RecurrenceEndDate = request.RecurrenceEndDate.HasValue ? (request.RecurrenceEndDate.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.RecurrenceEndDate.Value, DateTimeKind.Utc) : request.RecurrenceEndDate.Value) : task.RecurrenceEndDate;
        }
        task.UpdatedAt = DateTime.UtcNow;

        var statusChangedToDone = request.Status != null && string.Equals(request.Status.Trim(), "Done", StringComparison.OrdinalIgnoreCase) && !string.Equals(oldStatus, "Done", StringComparison.OrdinalIgnoreCase);

        await _taskRepository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

        if (statusChangedToDone && task.IsRecurring)
            await CreateNextRecurrenceInstanceAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        if (request.LabelIds != null)
        {
            var projectLabels = await _labelRepository.GetByProjectIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
            var validIds = projectLabels.Select(l => l.Id).ToHashSet();
            var toSet = request.LabelIds.Where(x => x != Guid.Empty && validIds.Contains(x)).Distinct().ToList();
            await _taskLabelRepository.SetForTaskAsync(task.Id, toSet, cancellationToken).ConfigureAwait(false);
        }

        if (request.CustomFieldValues != null)
            await SaveTaskCustomFieldValuesAsync(task.Id, task.ProjectId, request.CustomFieldValues, cancellationToken).ConfigureAwait(false);

        if (request.Status != null && request.Status.Trim() != oldStatus)
            await LogActivityAsync(task.Id, currentUserId, "StatusChanged", oldStatus, task.Status, cancellationToken).ConfigureAwait(false);
        if (request.Priority != null && request.Priority.Trim() != oldPriority)
            await LogActivityAsync(task.Id, currentUserId, "PriorityChanged", oldPriority, task.Priority, cancellationToken).ConfigureAwait(false);
        if (request.AssignedToUserId != null && request.AssignedToUserId != oldAssignedTo)
            await LogActivityAsync(task.Id, currentUserId, "AssignedToChanged", oldAssignedTo?.ToString(), task.AssignedToUserId?.ToString(), cancellationToken).ConfigureAwait(false);

        var assigneeId = task.AssignedToUserId;
        if (assigneeId.HasValue && assigneeId.Value != currentUserId)
        {
            if (request.AssignedToUserId != null && request.AssignedToUserId != oldAssignedTo && assigneeId.Value != Guid.Empty)
                await _notificationService.CreateAsync(assigneeId.Value, NotificationService.TypeTaskAssigned, "Task assigned", $"You were assigned to: {task.Title}", task.Id, cancellationToken).ConfigureAwait(false);
            if (request.DueDate.HasValue && oldDueDate != task.DueDate)
                await _notificationService.CreateAsync(assigneeId.Value, NotificationService.TypeTaskDueDateChanged, "Due date changed", $"Due date was updated for: {task.Title}", task.Id, cancellationToken).ConfigureAwait(false);
            if (request.Status != null && request.Status.Trim() != oldStatus)
                await _notificationService.CreateAsync(assigneeId.Value, NotificationService.TypeTaskStatusChanged, "Status changed", $"Status changed to {task.Status} for: {task.Title}", task.Id, cancellationToken).ConfigureAwait(false);
        }

        if (request.AssignedToUserId.HasValue && request.AssignedToUserId.Value != Guid.Empty)
            _logger.LogInformation("Task assigned. TaskId: {TaskId}, AssignedToUserId: {AssignedToUserId}, UpdatedByUserId: {UserId}", id, request.AssignedToUserId, currentUserId);
        else
            _logger.LogInformation("Task updated. TaskId: {TaskId}, UpdatedByUserId: {UserId}", id, currentUserId);

        return (UpdateTaskResult.Success, null);
    }

    public async Task<DeleteTaskResult> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionDelete, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete task {TaskId} without Task.Delete", currentUserId, id);
            return DeleteTaskResult.Forbidden;
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task == null)
            return DeleteTaskResult.NotFound;

        var project = await _projectRepository.GetByIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            var (canWrite, writeError) = await CanWriteTasksInProjectAsync(project, currentUserId, cancellationToken).ConfigureAwait(false);
            if (!canWrite)
            {
                _logger.LogWarning("User {UserId} attempted to delete task {TaskId}: {Reason}", currentUserId, id, writeError);
                return DeleteTaskResult.Forbidden;
            }
        }

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete task {TaskId} without project Member/Owner role.", currentUserId, id);
            return DeleteTaskResult.Forbidden;
        }

        var attachments = await _attachmentRepository.GetByTaskIdAsync(id, cancellationToken).ConfigureAwait(false);
        foreach (var att in attachments)
        {
            try
            {
                await _attachmentFileStorage.DeleteAsync(att.FilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete attachment file {Path} for task {TaskId}", att.FilePath, id);
            }
        }
        await LogActivityAsync(id, currentUserId, "TaskDeleted", task.Title, null, cancellationToken).ConfigureAwait(false);
        await _taskRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Task deleted. TaskId: {TaskId}, DeletedByUserId: {UserId}", id, currentUserId);

        return DeleteTaskResult.Success;
    }

    private async Task LogActivityAsync(Guid taskId, Guid userId, string action, string? oldValue, string? newValue, CancellationToken cancellationToken)
    {
        var activity = new TaskActivity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Action = action,
            OldValue = oldValue != null && oldValue.Length > 1000 ? oldValue[..1000] : oldValue,
            NewValue = newValue != null && newValue.Length > 1000 ? newValue[..1000] : newValue,
            CreatedAt = DateTime.UtcNow
        };
        await _activityRepository.CreateAsync(activity, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Activity created. TaskId: {TaskId}, Action: {Action}, UserId: {UserId}", taskId, action, userId);
    }

    private async Task<bool> CanAccessProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m != null;
    }

    /// <summary>
    /// Lifecycle rules: Archived = read-only; Completed = only Admin can write tasks.
    /// </summary>
    private async Task<(bool Allowed, string? ErrorMessage)> CanWriteTasksInProjectAsync(Project project, Guid userId, CancellationToken cancellationToken)
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

    private async Task<bool> CanEditProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (await _permissionChecker.HasPermissionAsync(userId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return true;
        var m = await _memberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken).ConfigureAwait(false);
        return m != null && (m.Role == "Member" || m.Role == "Owner");
    }

    private async Task<IReadOnlyList<LabelResponse>> GetLabelsForTaskAsync(Guid taskId, Guid projectId, CancellationToken cancellationToken)
    {
        var labelIds = await _taskLabelRepository.GetLabelIdsByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (labelIds.Count == 0) return Array.Empty<LabelResponse>();
        var labels = await _labelRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        var idSet = labelIds.ToHashSet();
        return labels.Where(l => idSet.Contains(l.Id)).Select(l => new LabelResponse { Id = l.Id, Name = l.Name, Color = l.Color, ProjectId = l.ProjectId, CreatedAt = l.CreatedAt }).ToList();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<LabelResponse>>> GetLabelsByTaskIdsAsync(IReadOnlyList<Guid> taskIds, Guid projectId, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<LabelResponse>>();
        var projectLabels = await _labelRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        var labelMap = projectLabels.ToDictionary(l => l.Id);
        var result = new Dictionary<Guid, IReadOnlyList<LabelResponse>>();
        foreach (var taskId in taskIds)
        {
            var labelIds = await _taskLabelRepository.GetLabelIdsByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
            var list = labelIds.Where(id => labelMap.ContainsKey(id)).Select(id => new LabelResponse { Id = labelMap[id].Id, Name = labelMap[id].Name, Color = labelMap[id].Color, ProjectId = labelMap[id].ProjectId, CreatedAt = labelMap[id].CreatedAt }).ToList<LabelResponse>();
            result[taskId] = list;
        }
        return result;
    }

    /// <summary>
    /// When a recurring task is marked Done, create the next instance with DueDate set to the next occurrence.
    /// Safe: only runs when task is recurring and has valid pattern/interval; respects RecurrenceEndDate.
    /// </summary>
    private async Task CreateNextRecurrenceInstanceAsync(ProjectTask completedTask, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (!completedTask.IsRecurring || string.IsNullOrWhiteSpace(completedTask.RecurrencePattern) || !completedTask.RecurrenceInterval.HasValue || completedTask.RecurrenceInterval.Value < 1)
            return;
        var baseDate = completedTask.DueDate ?? completedTask.CreatedAt;
        var nextDue = ComputeNextRecurrenceDate(baseDate, completedTask.RecurrencePattern.Trim(), completedTask.RecurrenceInterval.Value);
        if (!nextDue.HasValue)
            return;
        if (completedTask.RecurrenceEndDate.HasValue && nextDue.Value > completedTask.RecurrenceEndDate.Value)
            return;
        var sourceId = completedTask.RecurrenceSourceTaskId ?? completedTask.Id;
        var nextTask = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = completedTask.ProjectId,
            Title = completedTask.Title,
            Description = completedTask.Description,
            Status = "ToDo",
            Priority = completedTask.Priority,
            AssignedToUserId = completedTask.AssignedToUserId,
            DueDate = nextDue.Value,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            EstimatedHours = completedTask.EstimatedHours,
            IsRecurring = true,
            RecurrencePattern = completedTask.RecurrencePattern,
            RecurrenceInterval = completedTask.RecurrenceInterval,
            RecurrenceEndDate = completedTask.RecurrenceEndDate,
            RecurrenceSourceTaskId = sourceId
        };
        await _taskRepository.CreateAsync(nextTask, cancellationToken).ConfigureAwait(false);
        var labelIds = await _taskLabelRepository.GetLabelIdsByTaskIdAsync(completedTask.Id, cancellationToken).ConfigureAwait(false);
        if (labelIds.Count > 0)
            await _taskLabelRepository.SetForTaskAsync(nextTask.Id, labelIds, cancellationToken).ConfigureAwait(false);
        var customVals = await _taskCustomFieldValueRepository.GetByTaskIdAsync(completedTask.Id, cancellationToken).ConfigureAwait(false);
        if (customVals.Count > 0)
        {
            var toCopy = customVals.Select(v => (v.FieldId, v.Value)).ToList();
            await _taskCustomFieldValueRepository.SetForTaskAsync(nextTask.Id, toCopy, cancellationToken).ConfigureAwait(false);
        }
        await LogActivityAsync(nextTask.Id, currentUserId, "RecurrenceCreated", null, $"Next occurrence (from task {completedTask.Id})", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Recurring task next instance created. SourceTaskId: {SourceId}, NewTaskId: {NewId}, DueDate: {Due}", completedTask.Id, nextTask.Id, nextDue);
    }

    private static DateTime? ComputeNextRecurrenceDate(DateTime from, string pattern, int interval)
    {
        if (interval < 1) return null;
        var d = from.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(from, DateTimeKind.Utc) : from;
        return pattern.ToLowerInvariant() switch
        {
            "daily" => d.AddDays(interval),
            "weekly" => d.AddDays(7 * interval),
            "monthly" => d.AddMonths(interval),
            _ => null
        };
    }

    private async Task<IReadOnlyList<TaskCustomFieldValueResponse>> SaveTaskCustomFieldValuesAsync(Guid taskId, Guid projectId, IReadOnlyList<TaskCustomFieldValueItem>? values, CancellationToken cancellationToken)
    {
        if (values == null || values.Count == 0)
        {
            await _taskCustomFieldValueRepository.SetForTaskAsync(taskId, Array.Empty<(Guid, string?)>(), cancellationToken).ConfigureAwait(false);
            return Array.Empty<TaskCustomFieldValueResponse>();
        }
        var projectFields = await _projectCustomFieldRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        var validFieldIds = projectFields.Select(f => f.Id).ToHashSet();
        var toSave = values
            .Where(x => x.FieldId != Guid.Empty && validFieldIds.Contains(x.FieldId))
            .Select(x => (x.FieldId, x.Value))
            .ToList();
        await _taskCustomFieldValueRepository.SetForTaskAsync(taskId, toSave, cancellationToken).ConfigureAwait(false);
        return toSave.Select(x => new TaskCustomFieldValueResponse { FieldId = x.FieldId, Value = x.Value }).ToList();
    }

    private static TaskResponse MapToResponse(ProjectTask t, IReadOnlyList<LabelResponse> labels, IReadOnlyList<TaskCustomFieldValueResponse>? customFieldValues = null) => new()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        AssignedToUserId = t.AssignedToUserId,
        DueDate = t.DueDate,
        CreatedByUserId = t.CreatedByUserId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        EstimatedHours = t.EstimatedHours,
        IsRecurring = t.IsRecurring,
        RecurrencePattern = t.RecurrencePattern,
        RecurrenceInterval = t.RecurrenceInterval,
        RecurrenceEndDate = t.RecurrenceEndDate,
        RecurrenceSourceTaskId = t.RecurrenceSourceTaskId,
        Labels = labels ?? Array.Empty<LabelResponse>(),
        CustomFieldValues = customFieldValues ?? Array.Empty<TaskCustomFieldValueResponse>()
    };
}

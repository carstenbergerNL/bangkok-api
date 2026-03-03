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
    private readonly ILabelRepository _labelRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository taskRepository, IProjectRepository projectRepository, IProjectMemberRepository memberRepository, ITaskActivityRepository activityRepository, ITaskLabelRepository taskLabelRepository, ILabelRepository labelRepository, IUserPermissionChecker permissionChecker, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _activityRepository = activityRepository;
        _taskLabelRepository = taskLabelRepository;
        _labelRepository = labelRepository;
        _permissionChecker = permissionChecker;
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
        return (GetTaskResult.Ok, MapToResponse(task, labels));
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

        if (!await CanEditProjectAsync(request.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create task in project {ProjectId} without Member/Owner role.", currentUserId, request.ProjectId);
            return (CreateTaskResult.Forbidden, null, "You must be a project member (Member or Owner) to create tasks.");
        }

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
            CreatedAt = DateTime.UtcNow
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
        _logger.LogInformation("Task created. TaskId: {TaskId}, ProjectId: {ProjectId}, Title: {Title}, CreatedByUserId: {CreatedByUserId}", task.Id, task.ProjectId, task.Title, currentUserId);

        var labels = await GetLabelsForTaskAsync(task.Id, task.ProjectId, cancellationToken).ConfigureAwait(false);
        return (CreateTaskResult.Success, MapToResponse(task, labels), null);
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

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update task {TaskId} without project Member/Owner role.", currentUserId, id);
            return (UpdateTaskResult.Forbidden, "You must be a project member (Member or Owner) to edit tasks.");
        }

        var oldStatus = task.Status;
        var oldPriority = task.Priority;
        var oldAssignedTo = task.AssignedToUserId;

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
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

        if (request.LabelIds != null)
        {
            var projectLabels = await _labelRepository.GetByProjectIdAsync(task.ProjectId, cancellationToken).ConfigureAwait(false);
            var validIds = projectLabels.Select(l => l.Id).ToHashSet();
            var toSet = request.LabelIds.Where(x => x != Guid.Empty && validIds.Contains(x)).Distinct().ToList();
            await _taskLabelRepository.SetForTaskAsync(task.Id, toSet, cancellationToken).ConfigureAwait(false);
        }

        if (request.Status != null && request.Status.Trim() != oldStatus)
            await LogActivityAsync(task.Id, currentUserId, "StatusChanged", oldStatus, task.Status, cancellationToken).ConfigureAwait(false);
        if (request.Priority != null && request.Priority.Trim() != oldPriority)
            await LogActivityAsync(task.Id, currentUserId, "PriorityChanged", oldPriority, task.Priority, cancellationToken).ConfigureAwait(false);
        if (request.AssignedToUserId != null && request.AssignedToUserId != oldAssignedTo)
            await LogActivityAsync(task.Id, currentUserId, "AssignedToChanged", oldAssignedTo?.ToString(), task.AssignedToUserId?.ToString(), cancellationToken).ConfigureAwait(false);

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

        if (!await CanEditProjectAsync(task.ProjectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete task {TaskId} without project Member/Owner role.", currentUserId, id);
            return DeleteTaskResult.Forbidden;
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

    private static TaskResponse MapToResponse(ProjectTask t, IReadOnlyList<LabelResponse> labels) => new()
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
        Labels = labels ?? Array.Empty<LabelResponse>()
    };
}

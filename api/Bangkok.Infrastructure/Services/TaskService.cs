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

    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository taskRepository, IProjectRepository projectRepository, IUserPermissionChecker permissionChecker, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
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
        return (GetTaskResult.Ok, MapToResponse(task));
    }

    public async Task<IReadOnlyList<TaskResponse>> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list tasks without Task.View", currentUserId);
            return Array.Empty<TaskResponse>();
        }

        var tasks = await _taskRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        return tasks.Select(MapToResponse).ToList();
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
        _logger.LogInformation("Task created. TaskId: {TaskId}, ProjectId: {ProjectId}, Title: {Title}, CreatedByUserId: {CreatedByUserId}", task.Id, task.ProjectId, task.Title, currentUserId);

        return (CreateTaskResult.Success, MapToResponse(task), null);
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

        await _taskRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Task deleted. TaskId: {TaskId}, DeletedByUserId: {UserId}", id, currentUserId);

        return DeleteTaskResult.Success;
    }

    private static TaskResponse MapToResponse(ProjectTask t) => new()
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
        UpdatedAt = t.UpdatedAt
    };
}

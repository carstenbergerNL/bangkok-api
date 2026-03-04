using Bangkok.Application.Dto.TasksStandalone;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class TasksStandaloneService : ITasksStandaloneService
{
    private const string PermissionView = "Tasks.View";
    private const string PermissionCreate = "Tasks.Create";
    private const string PermissionEdit = "Tasks.Edit";
    private const string PermissionDelete = "Tasks.Delete";
    private const string PermissionAssign = "Tasks.Assign";

    private readonly ITasksStandaloneRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly ITenantUsageRepository _usageRepository;
    private readonly ILogger<TasksStandaloneService> _logger;

    public TasksStandaloneService(
        ITasksStandaloneRepository repository,
        ITenantContext tenantContext,
        ITenantUserRepository tenantUserRepository,
        IUserPermissionChecker permissionChecker,
        IUserRepository userRepository,
        ISubscriptionLimitService subscriptionLimitService,
        ITenantUsageRepository usageRepository,
        ILogger<TasksStandaloneService> logger)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _tenantUserRepository = tenantUserRepository;
        _permissionChecker = permissionChecker;
        _userRepository = userRepository;
        _subscriptionLimitService = subscriptionLimitService;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<(bool Success, IReadOnlyList<TasksStandaloneResponse>? Data, string? Error)> GetListAsync(Guid currentUserId, TasksStandaloneFilterRequest? filter, bool myTasksOnly, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list standalone tasks without Tasks.View.", currentUserId);
            return (false, null, "Tasks.View is required.");
        }

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        if (!await IsUserInTenantAsync(currentUserId, tenantId.Value, cancellationToken).ConfigureAwait(false))
            return (false, null, "You are not a member of this organization.");

        var assignedToOnly = myTasksOnly ? currentUserId : filter?.AssignedToUserId;
        var items = await _repository.GetListAsync(tenantId.Value, filter, assignedToOnly, cancellationToken).ConfigureAwait(false);
        var userIds = items.Select(x => x.AssignedToUserId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var displayNames = userIds.Count > 0
            ? await _userRepository.GetDisplayNamesByIdsAsync(userIds, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();

        var list = items.Select(t => MapToResponse(t, displayNames)).ToList();
        return (true, list, null);
    }

    public async Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to get standalone task without Tasks.View.", currentUserId);
            return (false, null, "Tasks.View is required.");
        }

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        if (!await IsUserInTenantAsync(currentUserId, tenantId.Value, cancellationToken).ConfigureAwait(false))
            return (false, null, "You are not a member of this organization.");

        var entity = await _repository.GetByIdAsync(id, tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return (false, null, "Task not found.");

        var displayNames = entity.AssignedToUserId.HasValue
            ? await _userRepository.GetDisplayNamesByIdsAsync(new[] { entity.AssignedToUserId.Value }, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();
        return (true, MapToResponse(entity, displayNames), null);
    }

    public async Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> CreateAsync(CreateTasksStandaloneRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionCreate, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create standalone task without Tasks.Create.", currentUserId);
            return (false, null, "Tasks.Create is required.");
        }

        if (request.AssignedToUserId.HasValue && !await _permissionChecker.HasPermissionAsync(currentUserId, PermissionAssign, cancellationToken).ConfigureAwait(false))
            return (false, null, "Tasks.Assign is required to assign a task.");

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        if (!await IsUserInTenantAsync(currentUserId, tenantId.Value, cancellationToken).ConfigureAwait(false))
            return (false, null, "You are not a member of this organization.");

        var (allowed, limitMessage) = await _subscriptionLimitService.CanCreateStandaloneTaskAsync(cancellationToken).ConfigureAwait(false);
        if (!allowed)
            return (false, null, limitMessage ?? "Task limit reached.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return (false, null, "Title is required.");

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Open" : request.Status.Trim();
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority.Trim();
        if (status != "Open" && status != "Completed") status = "Open";
        if (priority != "Low" && priority != "Medium" && priority != "High") priority = "Medium";

        var entity = new TasksStandalone
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = status,
            Priority = priority,
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = currentUserId,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
        await _usageRepository.EnsureExistsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        await _usageRepository.IncrementStandaloneTasksAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Standalone task created. TaskId: {TaskId}, TenantId: {TenantId}, Title: {Title}", entity.Id, tenantId.Value, entity.Title);
        var displayNames = entity.AssignedToUserId.HasValue
            ? await _userRepository.GetDisplayNamesByIdsAsync(new[] { entity.AssignedToUserId.Value }, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();
        return (true, MapToResponse(entity, displayNames), null);
    }

    public async Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> UpdateAsync(Guid id, UpdateTasksStandaloneRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update standalone task without Tasks.Edit.", currentUserId);
            return (false, null, "Tasks.Edit is required.");
        }

        if (request.AssignedToUserId.HasValue && !await _permissionChecker.HasPermissionAsync(currentUserId, PermissionAssign, cancellationToken).ConfigureAwait(false))
            return (false, null, "Tasks.Assign is required to assign a task.");

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        var entity = await _repository.GetByIdAsync(id, tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return (false, null, "Task not found.");

        if (entity.Status == "Completed")
        {
            if (request.Status != "Open" && request.Status != null)
                return (false, null, "Completed tasks can only be reopened (set status to Open).");
            if (request.Title != null || request.Description != null || request.Priority != null || request.DueDate != null || (request.AssignedToUserId.HasValue && request.AssignedToUserId != entity.AssignedToUserId))
                return (false, null, "Completed tasks are read-only except for reopening.");
        }

        if (request.Title != null) entity.Title = request.Title.Trim();
        if (request.Description != null) entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.Status != null)
        {
            var s = request.Status.Trim();
            if (s == "Open" || s == "Completed") entity.Status = s;
        }
        if (request.Priority != null)
        {
            var p = request.Priority.Trim();
            if (p == "Low" || p == "Medium" || p == "High") entity.Priority = p;
        }
        if (request.AssignedToUserId.HasValue) entity.AssignedToUserId = request.AssignedToUserId;
        if (request.DueDate.HasValue) entity.DueDate = request.DueDate;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Standalone task updated. TaskId: {TaskId}", id);
        var displayNames = entity.AssignedToUserId.HasValue
            ? await _userRepository.GetDisplayNamesByIdsAsync(new[] { entity.AssignedToUserId.Value }, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();
        return (true, MapToResponse(entity, displayNames), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionDelete, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete standalone task without Tasks.Delete.", currentUserId);
            return (false, "Tasks.Delete is required.");
        }

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, "Tenant context required.");

        var entity = await _repository.GetByIdAsync(id, tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return (false, "Task not found.");

        await _repository.DeleteAsync(id, tenantId.Value, cancellationToken).ConfigureAwait(false);
        await _usageRepository.DecrementStandaloneTasksAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Standalone task deleted. TaskId: {TaskId}", id);
        return (true, null);
    }

    public async Task<(bool Success, TasksStandaloneResponse? Data, string? Error)> SetStatusAsync(Guid id, string status, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
            return (false, null, "Tasks.Edit is required.");

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (false, null, "Tenant context required.");

        var entity = await _repository.GetByIdAsync(id, tenantId.Value, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return (false, null, "Task not found.");

        var s = status?.Trim() ?? "";
        if (s != "Open" && s != "Completed")
            return (false, null, "Status must be Open or Completed.");

        entity.Status = s;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

        var displayNames = entity.AssignedToUserId.HasValue
            ? await _userRepository.GetDisplayNamesByIdsAsync(new[] { entity.AssignedToUserId.Value }, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, string>();
        return (true, MapToResponse(entity, displayNames), null);
    }

    private async Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var members = await _tenantUserRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return members.Any(m => m.UserId == userId);
    }

    private static TasksStandaloneResponse MapToResponse(TasksStandalone t, IReadOnlyDictionary<Guid, string> displayNames)
    {
        return new TasksStandaloneResponse
        {
            Id = t.Id,
            TenantId = t.TenantId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToDisplayName = t.AssignedToUserId.HasValue && displayNames.TryGetValue(t.AssignedToUserId.Value, out var name) ? name : null,
            CreatedByUserId = t.CreatedByUserId,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}

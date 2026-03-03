using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private const string PermissionView = "Project.View";
    private const string PermissionCreate = "Project.Create";
    private const string PermissionEdit = "Project.Edit";
    private const string PermissionDelete = "Project.Delete";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IProjectTemplateRepository _templateRepository;
    private readonly IProjectTemplateTaskRepository _templateTaskRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ITenantContext _tenantContext;
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly ITenantUsageRepository _usageRepository;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository projectRepository, IProjectMemberRepository memberRepository, IProjectTemplateRepository templateRepository, IProjectTemplateTaskRepository templateTaskRepository, ITaskRepository taskRepository, IUserPermissionChecker permissionChecker, ITenantContext tenantContext, ISubscriptionLimitService subscriptionLimitService, ITenantUsageRepository usageRepository, ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _templateRepository = templateRepository;
        _templateTaskRepository = templateTaskRepository;
        _taskRepository = taskRepository;
        _permissionChecker = permissionChecker;
        _tenantContext = tenantContext;
        _subscriptionLimitService = subscriptionLimitService;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<(GetProjectResult Result, ProjectResponse? Data)> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to get project {ProjectId} without Project.View", currentUserId, id);
            return (GetProjectResult.Forbidden, null);
        }

        var project = await _projectRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (GetProjectResult.NotFound, null);

        var isAdmin = _tenantContext.IsPlatformAdmin || await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        if (!isAdmin)
        {
            if (_tenantContext.CurrentTenantId != project.TenantId)
            {
                _logger.LogWarning("User {UserId} attempted to get project {ProjectId} from another tenant.", currentUserId, id);
                return (GetProjectResult.Forbidden, null);
            }
            var membership = await _memberRepository.GetByProjectAndUserAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
            if (membership == null)
            {
                _logger.LogWarning("User {UserId} attempted to get project {ProjectId} without membership.", currentUserId, id);
                return (GetProjectResult.Forbidden, null);
            }
        }

        return (GetProjectResult.Ok, MapToResponse(project));
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(Guid currentUserId, string? status = null, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list projects without Project.View", currentUserId);
            return Array.Empty<ProjectResponse>();
        }

        var isAdmin = _tenantContext.IsPlatformAdmin || await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        var tenantId = isAdmin ? null : _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue && !isAdmin)
            return Array.Empty<ProjectResponse>();

        var allProjects = await _projectRepository.GetAllAsync(tenantId, status, cancellationToken).ConfigureAwait(false);
        if (isAdmin)
            return allProjects.Select(MapToResponse).ToList();

        var memberProjectIds = await _memberRepository.GetProjectIdsByUserIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        var allowed = new HashSet<Guid>(memberProjectIds);
        return allProjects.Where(p => allowed.Contains(p.Id)).Select(MapToResponse).ToList();
    }

    public async Task<(CreateProjectResult Result, ProjectResponse? Data, string? ErrorMessage)> CreateAsync(CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionCreate, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create project without Project.Create", currentUserId);
            return (CreateProjectResult.Forbidden, null, "You do not have permission to create projects.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (CreateProjectResult.ValidationError, null, "Name is required.");

        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("User {UserId} attempted to create project without a tenant context.", currentUserId);
            return (CreateProjectResult.Forbidden, null, "Tenant context is required. Please select a tenant or sign in again.");
        }

        var (canCreate, limitMsg) = await _subscriptionLimitService.CanCreateProjectAsync(cancellationToken).ConfigureAwait(false);
        if (!canCreate)
        {
            _logger.LogWarning("User {UserId} blocked by subscription limit creating project.", currentUserId);
            return (CreateProjectResult.Forbidden, null, limitMsg ?? "Project limit reached. Upgrade your plan.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.CreateAsync(project, cancellationToken).ConfigureAwait(false);
        await _usageRepository.IncrementProjectsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);

        var ownerMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = currentUserId,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        };
        await _memberRepository.AddAsync(ownerMember, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Project created. ProjectId: {ProjectId}, Name: {Name}, CreatedByUserId: {CreatedByUserId}", project.Id, project.Name, currentUserId);

        return (CreateProjectResult.Success, MapToResponse(project), null);
    }

    public async Task<(CreateProjectResult Result, ProjectResponse? Data, string? ErrorMessage)> CreateFromTemplateAsync(Guid templateId, CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionCreate, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create project from template without Project.Create", currentUserId);
            return (CreateProjectResult.Forbidden, null, "You do not have permission to create projects.");
        }
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken).ConfigureAwait(false);
        if (template == null)
            return (CreateProjectResult.ValidationError, null, "Template not found.");
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (CreateProjectResult.ValidationError, null, "Name is required.");
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
            return (CreateProjectResult.Forbidden, null, "Tenant context is required. Please select a tenant or sign in again.");
        var (canCreate, limitMsg) = await _subscriptionLimitService.CanCreateProjectAsync(cancellationToken).ConfigureAwait(false);
        if (!canCreate)
        {
            _logger.LogWarning("User {UserId} blocked by subscription limit creating project from template.", currentUserId);
            return (CreateProjectResult.Forbidden, null, limitMsg ?? "Project limit reached. Upgrade your plan.");
        }
        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _projectRepository.CreateAsync(project, cancellationToken).ConfigureAwait(false);
        await _usageRepository.IncrementProjectsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var ownerMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = currentUserId,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow
        };
        await _memberRepository.AddAsync(ownerMember, cancellationToken).ConfigureAwait(false);
        var templateTasks = await _templateTaskRepository.GetByTemplateIdAsync(templateId, cancellationToken).ConfigureAwait(false);
        foreach (var tt in templateTasks)
        {
            var task = new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = tt.Title,
                Description = tt.Description,
                Status = string.IsNullOrWhiteSpace(tt.DefaultStatus) ? "ToDo" : tt.DefaultStatus,
                Priority = string.IsNullOrWhiteSpace(tt.DefaultPriority) ? "Medium" : tt.DefaultPriority,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            await _taskRepository.CreateAsync(task, cancellationToken).ConfigureAwait(false);
        }
        _logger.LogInformation("Project created from template. ProjectId: {ProjectId}, TemplateId: {TemplateId}, Tasks: {Count}", project.Id, templateId, templateTasks.Count);
        return (CreateProjectResult.Success, MapToResponse(project), null);
    }

    public async Task<(UpdateProjectResult Result, string? ErrorMessage)> UpdateAsync(Guid id, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to update project {ProjectId} without Project.Edit", currentUserId, id);
            return (UpdateProjectResult.Forbidden, "You do not have permission to edit projects.");
        }

        var project = await _projectRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (UpdateProjectResult.NotFound, null);

        var isAdmin = _tenantContext.IsPlatformAdmin || await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        if (!isAdmin && _tenantContext.CurrentTenantId != project.TenantId)
        {
            _logger.LogWarning("User {UserId} attempted to update project {ProjectId} from another tenant.", currentUserId, id);
            return (UpdateProjectResult.Forbidden, "You do not have access to this project.");
        }
        if (!isAdmin)
        {
            var membership = await _memberRepository.GetByProjectAndUserAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
            if (membership == null || membership.Role != "Owner")
            {
                _logger.LogWarning("User {UserId} attempted to update project {ProjectId} without owner role.", currentUserId, id);
                return (UpdateProjectResult.Forbidden, "Only project owners or admins can edit the project.");
            }
        }

        if (request.Name != null)
            project.Name = request.Name.Trim();
        if (request.Description != null)
            project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.Status != null)
            project.Status = request.Status.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project updated. ProjectId: {ProjectId}, UpdatedByUserId: {UserId}", id, currentUserId);

        return (UpdateProjectResult.Success, null);
    }

    public async Task<DeleteProjectResult> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionDelete, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete project {ProjectId} without Project.Delete", currentUserId, id);
            return DeleteProjectResult.Forbidden;
        }

        var project = await _projectRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return DeleteProjectResult.NotFound;

        var isAdmin = _tenantContext.IsPlatformAdmin || await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false);
        if (!isAdmin && _tenantContext.CurrentTenantId != project.TenantId)
        {
            _logger.LogWarning("User {UserId} attempted to delete project {ProjectId} from another tenant.", currentUserId, id);
            return DeleteProjectResult.Forbidden;
        }
        if (!isAdmin)
        {
            var membership = await _memberRepository.GetByProjectAndUserAsync(id, currentUserId, cancellationToken).ConfigureAwait(false);
            if (membership == null || membership.Role != "Owner")
            {
                _logger.LogWarning("User {UserId} attempted to delete project {ProjectId} without owner role.", currentUserId, id);
                return DeleteProjectResult.Forbidden;
            }
        }

        var taskCount = await _projectRepository.GetTaskCountByProjectIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (taskCount > 0)
            return DeleteProjectResult.HasTasks;

        await _memberRepository.DeleteByProjectIdAsync(id, cancellationToken).ConfigureAwait(false);
        await _projectRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        await _usageRepository.DecrementProjectsAsync(project.TenantId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project deleted. ProjectId: {ProjectId}, DeletedByUserId: {UserId}", id, currentUserId);

        return DeleteProjectResult.Success;
    }

    private static ProjectResponse MapToResponse(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Status = p.Status,
        CreatedByUserId = p.CreatedByUserId,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}

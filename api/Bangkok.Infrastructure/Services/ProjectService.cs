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

    private readonly IProjectRepository _projectRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository projectRepository, IUserPermissionChecker permissionChecker, ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _permissionChecker = permissionChecker;
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
        return (GetProjectResult.Ok, MapToResponse(project));
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionView, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list projects without Project.View", currentUserId);
            return Array.Empty<ProjectResponse>();
        }

        var projects = await _projectRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return projects.Select(MapToResponse).ToList();
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

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim(),
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.CreateAsync(project, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project created. ProjectId: {ProjectId}, Name: {Name}, CreatedByUserId: {CreatedByUserId}", project.Id, project.Name, currentUserId);

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

        var taskCount = await _projectRepository.GetTaskCountByProjectIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (taskCount > 0)
            return DeleteProjectResult.HasTasks;

        await _projectRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
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

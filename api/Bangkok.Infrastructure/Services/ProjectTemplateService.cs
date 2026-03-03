using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectTemplateService : IProjectTemplateService
{
    private const string AdminPermission = "ViewAdminSettings";
    private const string PermissionCreateProject = "Project.Create";

    private readonly IProjectTemplateRepository _templateRepository;
    private readonly IProjectTemplateTaskRepository _templateTaskRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<ProjectTemplateService> _logger;

    public ProjectTemplateService(
        IProjectTemplateRepository templateRepository,
        IProjectTemplateTaskRepository templateTaskRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<ProjectTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _templateTaskRepository = templateTaskRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProjectTemplateResponse>> GetAllAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false)
            && !await _permissionChecker.HasPermissionAsync(currentUserId, PermissionCreateProject, cancellationToken).ConfigureAwait(false))
            return Array.Empty<ProjectTemplateResponse>();
        var templates = await _templateRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<ProjectTemplateResponse>();
        foreach (var t in templates)
        {
            var tasks = await _templateTaskRepository.GetByTemplateIdAsync(t.Id, cancellationToken).ConfigureAwait(false);
            result.Add(MapToResponse(t, tasks));
        }
        return result;
    }

    public async Task<ProjectTemplateResponse?> GetByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return null;
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template == null) return null;
        var tasks = await _templateTaskRepository.GetByTemplateIdAsync(id, cancellationToken).ConfigureAwait(false);
        return MapToResponse(template, tasks);
    }

    public async Task<(bool Success, ProjectTemplateResponse? Data, string? Error)> CreateAsync(CreateProjectTemplateRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return (false, null, "You do not have permission to manage templates.");
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name is required.");
        var template = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _templateRepository.CreateAsync(template, cancellationToken).ConfigureAwait(false);
        var taskList = new List<ProjectTemplateTask>();
        if (request.Tasks != null)
        {
            foreach (var req in request.Tasks)
            {
                if (string.IsNullOrWhiteSpace(req?.Title)) continue;
                var tt = new ProjectTemplateTask
                {
                    Id = Guid.NewGuid(),
                    TemplateId = template.Id,
                    Title = req.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                    DefaultStatus = string.IsNullOrWhiteSpace(req.DefaultStatus) ? null : req.DefaultStatus.Trim(),
                    DefaultPriority = string.IsNullOrWhiteSpace(req.DefaultPriority) ? null : req.DefaultPriority.Trim()
                };
                await _templateTaskRepository.CreateAsync(tt, cancellationToken).ConfigureAwait(false);
                taskList.Add(tt);
            }
        }
        _logger.LogInformation("Project template created. TemplateId: {Id}, Name: {Name}", template.Id, template.Name);
        return (true, MapToResponse(template, taskList), null);
    }

    public async Task<(bool Success, ProjectTemplateResponse? Data, string? Error)> UpdateAsync(Guid id, UpdateProjectTemplateRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return (false, null, "You do not have permission to manage templates.");
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name is required.");
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template == null)
            return (false, null, "Template not found.");
        template.Name = request.Name.Trim();
        template.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await _templateRepository.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
        await _templateTaskRepository.DeleteByTemplateIdAsync(id, cancellationToken).ConfigureAwait(false);
        var taskList = new List<ProjectTemplateTask>();
        if (request.Tasks != null)
        {
            foreach (var req in request.Tasks)
            {
                if (string.IsNullOrWhiteSpace(req?.Title)) continue;
                var tt = new ProjectTemplateTask
                {
                    Id = Guid.NewGuid(),
                    TemplateId = id,
                    Title = req.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                    DefaultStatus = string.IsNullOrWhiteSpace(req.DefaultStatus) ? null : req.DefaultStatus.Trim(),
                    DefaultPriority = string.IsNullOrWhiteSpace(req.DefaultPriority) ? null : req.DefaultPriority.Trim()
                };
                await _templateTaskRepository.CreateAsync(tt, cancellationToken).ConfigureAwait(false);
                taskList.Add(tt);
            }
        }
        _logger.LogInformation("Project template updated. TemplateId: {Id}, Name: {Name}", id, template.Name);
        return (true, MapToResponse(template, taskList), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, AdminPermission, cancellationToken).ConfigureAwait(false))
            return (false, "You do not have permission to manage templates.");
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template == null)
            return (false, "Template not found.");
        await _templateRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project template deleted. TemplateId: {Id}", id);
        return (true, null);
    }

    private static ProjectTemplateResponse MapToResponse(ProjectTemplate t, IReadOnlyList<ProjectTemplateTask> tasks) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        CreatedAt = t.CreatedAt,
        Tasks = tasks.Select(tk => new ProjectTemplateTaskResponse
        {
            Id = tk.Id,
            TemplateId = tk.TemplateId,
            Title = tk.Title,
            Description = tk.Description,
            DefaultStatus = tk.DefaultStatus,
            DefaultPriority = tk.DefaultPriority
        }).ToList()
    };
}

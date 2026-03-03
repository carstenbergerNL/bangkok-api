using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectCustomFieldService : IProjectCustomFieldService
{
    private const string PermissionProjectEdit = "Project.Edit";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly IProjectCustomFieldRepository _fieldRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<ProjectCustomFieldService> _logger;

    public ProjectCustomFieldService(
        IProjectCustomFieldRepository fieldRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<ProjectCustomFieldService> logger)
    {
        _fieldRepository = fieldRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<(bool Success, IReadOnlyList<ProjectCustomFieldResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanAccessProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "You do not have access to this project.");

        var fields = await _fieldRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        return (true, fields.Select(MapToResponse).ToList(), null);
    }

    public async Task<(bool Success, ProjectCustomFieldResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateProjectCustomFieldRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
            return (false, null, "Project.Edit is required to manage custom fields.");

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "You do not have permission to manage custom fields in this project.");

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name is required.");

        var fieldType = string.IsNullOrWhiteSpace(request.FieldType) ? "Text" : request.FieldType.Trim();
        if (!IsValidFieldType(fieldType))
            return (false, null, "FieldType must be Text, Number, Date, or Dropdown.");

        var field = new ProjectCustomField
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            FieldType = fieldType,
            Options = string.IsNullOrWhiteSpace(request.Options) ? null : request.Options.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _fieldRepository.CreateAsync(field, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project custom field created. FieldId: {FieldId}, ProjectId: {ProjectId}, Name: {Name}", field.Id, projectId, field.Name);
        return (true, MapToResponse(field), null);
    }

    public async Task<(bool Success, ProjectCustomFieldResponse? Data, string? Error)> UpdateAsync(Guid projectId, Guid fieldId, UpdateProjectCustomFieldRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
            return (false, null, "Project.Edit is required to manage custom fields.");

        var field = await _fieldRepository.GetByIdAsync(fieldId, cancellationToken).ConfigureAwait(false);
        if (field == null || field.ProjectId != projectId)
            return (false, null, "Custom field not found.");

        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, null, "You do not have permission to manage custom fields in this project.");

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name is required.");

        var fieldType = string.IsNullOrWhiteSpace(request.FieldType) ? "Text" : request.FieldType.Trim();
        if (!IsValidFieldType(fieldType))
            return (false, null, "FieldType must be Text, Number, Date, or Dropdown.");

        field.Name = request.Name.Trim();
        field.FieldType = fieldType;
        field.Options = string.IsNullOrWhiteSpace(request.Options) ? null : request.Options.Trim();
        await _fieldRepository.UpdateAsync(field, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project custom field updated. FieldId: {FieldId}, ProjectId: {ProjectId}", fieldId, projectId);
        return (true, MapToResponse(field), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid fieldId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
            return (false, "Project.Edit is required to manage custom fields.");

        var field = await _fieldRepository.GetByIdAsync(fieldId, cancellationToken).ConfigureAwait(false);
        if (field == null || field.ProjectId != projectId)
            return (false, "Custom field not found.");

        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
            return (false, "You do not have permission to manage custom fields in this project.");

        await _fieldRepository.DeleteAsync(fieldId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Project custom field deleted. FieldId: {FieldId}, ProjectId: {ProjectId}", fieldId, projectId);
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

    private static bool IsValidFieldType(string type)
    {
        return type.Equals("Text", StringComparison.OrdinalIgnoreCase)
            || type.Equals("Number", StringComparison.OrdinalIgnoreCase)
            || type.Equals("Date", StringComparison.OrdinalIgnoreCase)
            || type.Equals("Dropdown", StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectCustomFieldResponse MapToResponse(ProjectCustomField f) => new()
    {
        Id = f.Id,
        ProjectId = f.ProjectId,
        Name = f.Name,
        FieldType = f.FieldType,
        Options = f.Options,
        CreatedAt = f.CreatedAt
    };
}

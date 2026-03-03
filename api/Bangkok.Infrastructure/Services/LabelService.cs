using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class LabelService : ILabelService
{
    private const string PermissionProjectEdit = "Project.Edit";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly ILabelRepository _labelRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ILogger<LabelService> _logger;

    public LabelService(
        ILabelRepository labelRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserPermissionChecker permissionChecker,
        ILogger<LabelService> logger)
    {
        _labelRepository = labelRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    public async Task<(bool Success, IReadOnlyList<LabelResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanAccessProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list labels for project {ProjectId} without access.", currentUserId, projectId);
            return (false, null, "You do not have access to this project.");
        }

        var labels = await _labelRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        return (true, labels.Select(MapToResponse).ToList(), null);
    }

    public async Task<(bool Success, LabelResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create label without Project.Edit.", currentUserId);
            return (false, null, "Project.Edit is required to manage labels.");
        }

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");

        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create label in project {ProjectId} without edit access.", currentUserId, projectId);
            return (false, null, "You do not have permission to manage labels in this project.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name is required.");
        if (string.IsNullOrWhiteSpace(request.Color))
            return (false, null, "Color is required.");

        var label = new Label
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Color = request.Color.Trim(),
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow
        };
        await _labelRepository.AddAsync(label, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Label created. LabelId: {LabelId}, ProjectId: {ProjectId}, Name: {Name}", label.Id, projectId, label.Name);
        return (true, MapToResponse(label), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete label without Project.Edit.", currentUserId);
            return (false, "Project.Edit is required to manage labels.");
        }

        var label = await _labelRepository.GetByIdAsync(labelId, cancellationToken).ConfigureAwait(false);
        if (label == null)
            return (false, "Label not found.");
        if (label.ProjectId != projectId)
        {
            _logger.LogWarning("User {UserId} attempted to delete label {LabelId} from wrong project.", currentUserId, labelId);
            return (false, "Label not found.");
        }

        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete label in project {ProjectId} without edit access.", currentUserId, projectId);
            return (false, "You do not have permission to manage labels in this project.");
        }

        await _labelRepository.DeleteAsync(labelId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Label deleted. LabelId: {LabelId}, ProjectId: {ProjectId}", labelId, projectId);
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

    private static LabelResponse MapToResponse(Label l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Color = l.Color,
        ProjectId = l.ProjectId,
        CreatedAt = l.CreatedAt
    };
}

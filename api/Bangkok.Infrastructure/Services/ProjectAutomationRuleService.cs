using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProjectAutomationRuleService : IProjectAutomationRuleService
{
    private const string PermissionProjectEdit = "Project.Edit";
    private const string AdminPermission = "ViewAdminSettings";

    private static readonly HashSet<string> ValidTriggers = new(StringComparer.OrdinalIgnoreCase) { "TaskCompleted", "TaskOverdue", "TaskAssigned" };
    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase) { "NotifyUser", "ChangeStatus", "AddLabel" };

    private readonly IProjectAutomationRuleRepository _ruleRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUserPermissionChecker _permissionChecker;
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly ILogger<ProjectAutomationRuleService> _logger;

    public ProjectAutomationRuleService(
        IProjectAutomationRuleRepository ruleRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUserPermissionChecker permissionChecker,
        ISubscriptionLimitService subscriptionLimitService,
        ILogger<ProjectAutomationRuleService> logger)
    {
        _ruleRepository = ruleRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _permissionChecker = permissionChecker;
        _subscriptionLimitService = subscriptionLimitService;
        _logger = logger;
    }

    public async Task<(bool Success, IReadOnlyList<ProjectAutomationRuleResponse>? Data, string? Error)> GetByProjectIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");
        if (!await CanAccessProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to list automation rules for project {ProjectId} without access.", currentUserId, projectId);
            return (false, null, "You do not have access to this project.");
        }
        var rules = await _ruleRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        return (true, rules.Select(MapToResponse).ToList(), null);
    }

    public async Task<(bool Success, ProjectAutomationRuleResponse? Data, string? Error)> CreateAsync(Guid projectId, CreateProjectAutomationRuleRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create automation rule without Project.Edit.", currentUserId);
            return (false, null, "Project.Edit is required.");
        }
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
            return (false, null, "Project not found.");
        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to create automation rule in project {ProjectId} without edit access.", currentUserId, projectId);
            return (false, null, "You do not have permission to manage automation in this project.");
        }
        if (request == null)
            return (false, null, "Request is required.");
        var trigger = (request.Trigger ?? "").Trim();
        var action = (request.Action ?? "").Trim();
        if (string.IsNullOrEmpty(trigger) || !ValidTriggers.Contains(trigger))
            return (false, null, "Invalid trigger. Use: TaskCompleted, TaskOverdue, TaskAssigned.");
        if (string.IsNullOrEmpty(action) || !ValidActions.Contains(action))
            return (false, null, "Invalid action. Use: NotifyUser, ChangeStatus, AddLabel.");
        if (string.Equals(action, "NotifyUser", StringComparison.OrdinalIgnoreCase) && (!request.TargetUserId.HasValue || request.TargetUserId.Value == Guid.Empty))
            return (false, null, "NotifyUser requires TargetUserId.");
        if (string.Equals(action, "ChangeStatus", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.TargetValue))
            return (false, null, "ChangeStatus requires TargetValue (e.g. Done, ToDo).");
        if (string.Equals(action, "AddLabel", StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(request.TargetValue) || !Guid.TryParse(request.TargetValue.Trim(), out _)))
            return (false, null, "AddLabel requires TargetValue (label id).");

        var (canUseAutomation, limitMsg) = await _subscriptionLimitService.CanUseAutomationAsync(cancellationToken).ConfigureAwait(false);
        if (!canUseAutomation)
        {
            _logger.LogWarning("User {UserId} blocked by subscription creating automation rule in project {ProjectId}.", currentUserId, projectId);
            return (false, null, limitMsg ?? "Automation is not included in your plan.");
        }

        var rule = new ProjectAutomationRule
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Trigger = trigger,
            Action = action,
            TargetUserId = request.TargetUserId == Guid.Empty ? null : request.TargetUserId,
            TargetValue = string.IsNullOrWhiteSpace(request.TargetValue) ? null : request.TargetValue.Trim()
        };
        await _ruleRepository.CreateAsync(rule, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Automation rule created. RuleId: {RuleId}, ProjectId: {ProjectId}, Trigger: {Trigger}, Action: {Action}", rule.Id, projectId, trigger, action);
        return (true, MapToResponse(rule), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid projectId, Guid ruleId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!await _permissionChecker.HasPermissionAsync(currentUserId, PermissionProjectEdit, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete automation rule without Project.Edit.", currentUserId);
            return (false, "Project.Edit is required.");
        }
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule == null)
            return (false, "Rule not found.");
        if (rule.ProjectId != projectId)
            return (false, "Rule not found.");
        if (!await CanEditProjectAsync(projectId, currentUserId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("User {UserId} attempted to delete automation rule in project {ProjectId} without edit access.", currentUserId, projectId);
            return (false, "You do not have permission to manage automation in this project.");
        }
        await _ruleRepository.DeleteAsync(ruleId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Automation rule deleted. RuleId: {RuleId}, ProjectId: {ProjectId}", ruleId, projectId);
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

    private static ProjectAutomationRuleResponse MapToResponse(ProjectAutomationRule r) => new()
    {
        Id = r.Id,
        ProjectId = r.ProjectId,
        Trigger = r.Trigger,
        Action = r.Action,
        TargetUserId = r.TargetUserId,
        TargetValue = r.TargetValue
    };
}

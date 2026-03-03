using System.Security.Claims;
using Bangkok.Application.Dto.Projects;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("Project planning. All endpoints require Bearer token. Permission-based: Project.View, Project.Create, Project.Edit, Project.Delete. Returns 403 when permission missing.")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectMemberService _memberService;
    private readonly ILabelService _labelService;
    private readonly IProjectCustomFieldService _customFieldService;
    private readonly IProjectExportService _exportService;
    private readonly IProjectDashboardService _dashboardService;
    private readonly IProjectAutomationRuleService _automationRuleService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, IProjectMemberService memberService, ILabelService labelService, IProjectCustomFieldService customFieldService, IProjectExportService exportService, IProjectDashboardService dashboardService, IProjectAutomationRuleService automationRuleService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _memberService = memberService;
        _labelService = labelService;
        _customFieldService = customFieldService;
        _exportService = exportService;
        _dashboardService = dashboardService;
        _automationRuleService = automationRuleService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List projects", Description = "Returns all projects. Requires Project.View. Returns 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectResponse>>>> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _projectService.GetAllAsync(currentUserId.Value, status, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<ProjectResponse>>.Ok(list, correlationId));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get project by ID", Description = "Returns a single project. Requires Project.View. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, data) = await _projectService.GetByIdAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == GetProjectResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have permission to view projects." }, correlationId));
        if (result == GetProjectResult.NotFound || data == null)
            return NotFound(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = "Project not found." }, correlationId));

        return Ok(ApiResponse<ProjectResponse>.Ok(data, correlationId));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create project", Description = "Creates a new project. Requires Project.Create. Returns 201 with created resource. 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, data, errorMessage) = await _projectService.CreateAsync(request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == CreateProjectResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to create projects." }, correlationId));
        if (result == CreateProjectResult.ValidationError)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = errorMessage ?? "Invalid request." }, correlationId));
        if (data == null)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "ERROR", Message = "Create failed." }, correlationId));

        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<ProjectResponse>.Ok(data, correlationId));
    }

    [HttpPost("from-template/{templateId:guid}")]
    [SwaggerOperation(Summary = "Create project from template", Description = "Creates a new project with name/description/status from request body, then copies all template tasks into the project. Requires Project.Create.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> CreateFromTemplate([FromRoute] Guid templateId, [FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, data, errorMessage) = await _projectService.CreateFromTemplateAsync(templateId, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == CreateProjectResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to create projects." }, correlationId));
        if (result == CreateProjectResult.ValidationError)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = errorMessage ?? "Invalid request or template not found." }, correlationId));
        if (data == null)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "ERROR", Message = "Create failed." }, correlationId));

        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<ProjectResponse>.Ok(data, correlationId));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update project", Description = "Updates an existing project. Requires Project.Edit. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, errorMessage) = await _projectService.UpdateAsync(id, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == UpdateProjectResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to edit projects." }, correlationId));
        if (result == UpdateProjectResult.NotFound)
            return NotFound(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = "Project not found." }, correlationId));
        if (result == UpdateProjectResult.ValidationError)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = errorMessage ?? "Invalid request." }, correlationId));

        var (_, updated) = await _projectService.GetByIdAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<ProjectResponse>.Ok(updated!, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete project", Description = "Deletes a project. Requires Project.Delete. Cannot delete if project has tasks (400). 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var result = await _projectService.DeleteAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == DeleteProjectResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have permission to delete projects." }, correlationId));
        if (result == DeleteProjectResult.NotFound)
            return NotFound(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = "Project not found." }, correlationId));
        if (result == DeleteProjectResult.HasTasks)
            return BadRequest(ApiResponse<ProjectResponse>.Fail(new ErrorResponse { Code = "PROJECT_HAS_TASKS", Message = "Cannot delete project that has tasks. Delete or reassign tasks first." }, correlationId));

        return NoContent();
    }

    [HttpGet("{projectId:guid}/dashboard")]
    [SwaggerOperation(Summary = "Get project dashboard", Description = "Returns dashboard stats: total/completed/overdue tasks, tasks per status, tasks per member. Requires project access. 403 if not allowed.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDashboardResponse>>> GetDashboard([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectDashboardResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _dashboardService.GetDashboardAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectDashboardResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectDashboardResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        }
        return Ok(ApiResponse<ProjectDashboardResponse>.Ok(data!, correlationId));
    }

    [HttpGet("{projectId:guid}/members/me")]
    [SwaggerOperation(Summary = "Get current user's project role", Description = "Returns the current user's role (Owner, Member, Viewer) for the project. Admin is treated as Owner. 403 if not a member.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetMyRole([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, role, error) = await _memberService.GetCurrentUserRoleAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        }
        return Ok(ApiResponse<object>.Ok(new { role }, correlationId));
    }

    [HttpGet("{projectId:guid}/members")]
    [SwaggerOperation(Summary = "List project members", Description = "Returns members of the project. Requires project membership or Admin. 403 if not allowed.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectMemberResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectMemberResponse>>>> GetMembers([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectMemberResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _memberService.GetByProjectIdAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<IReadOnlyList<ProjectMemberResponse>>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ProjectMemberResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
        }
        return Ok(ApiResponse<IReadOnlyList<ProjectMemberResponse>>.Ok(data ?? Array.Empty<ProjectMemberResponse>(), correlationId));
    }

    [HttpPost("{projectId:guid}/members")]
    [SwaggerOperation(Summary = "Add project member", Description = "Adds a user as member. Only Owner or Admin. 403 if not allowed.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectMemberResponse>>> AddMember([FromRoute] Guid projectId, [FromBody] CreateProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectMemberResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _memberService.AddAsync(projectId, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectMemberResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("owners or admins") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectMemberResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectMemberResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectMemberResponse>.Ok(data!, correlationId));
    }

    [HttpPut("{projectId:guid}/members/{memberId:guid}")]
    [SwaggerOperation(Summary = "Update project member role", Description = "Changes a member's role. Only Owner or Admin. Cannot remove last owner. 403 if not allowed.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember([FromRoute] Guid projectId, [FromRoute] Guid memberId, [FromBody] UpdateProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _memberService.UpdateRoleAsync(projectId, memberId, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "MEMBER_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("owners or admins") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
        }
        return Ok(ApiResponse<object>.Ok(null, correlationId));
    }

    [HttpDelete("{projectId:guid}/members/{memberId:guid}")]
    [SwaggerOperation(Summary = "Remove project member", Description = "Removes a member. Only Owner or Admin. Cannot remove last owner. 403 if not allowed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid projectId, [FromRoute] Guid memberId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _memberService.RemoveAsync(projectId, memberId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "MEMBER_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("owners or admins") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
        }
        return NoContent();
    }

    [HttpGet("{projectId:guid}/labels")]
    [SwaggerOperation(Summary = "List project labels", Description = "Returns labels for the project. Requires project access. 403 if not allowed.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabelResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LabelResponse>>>> GetLabels([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<LabelResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _labelService.GetByProjectIdAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<IReadOnlyList<LabelResponse>>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<LabelResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
        }
        return Ok(ApiResponse<IReadOnlyList<LabelResponse>>.Ok(data ?? Array.Empty<LabelResponse>(), correlationId));
    }

    [HttpPost("{projectId:guid}/labels")]
    [SwaggerOperation(Summary = "Create label", Description = "Creates a label for the project. Requires Project.Edit and project member (Member/Owner) or Admin. 403 if not allowed.")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LabelResponse>>> CreateLabel([FromRoute] Guid projectId, [FromBody] CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<LabelResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _labelService.CreateAsync(projectId, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<LabelResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<LabelResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<LabelResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<LabelResponse>.Ok(data!, correlationId));
    }

    [HttpDelete("{projectId:guid}/labels/{labelId:guid}")]
    [SwaggerOperation(Summary = "Delete label", Description = "Deletes a label. Requires Project.Edit and project member (Member/Owner) or Admin. 403 if not allowed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel([FromRoute] Guid projectId, [FromRoute] Guid labelId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _labelService.DeleteAsync(projectId, labelId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "LABEL_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
        }
        return NoContent();
    }

    [HttpGet("{projectId:guid}/automation-rules")]
    [SwaggerOperation(Summary = "List automation rules", Description = "Returns automation rules for the project. Requires project access.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>>> GetAutomationRules([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _automationRuleService.GetByProjectIdAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
        }
        return Ok(ApiResponse<IReadOnlyList<ProjectAutomationRuleResponse>>.Ok(data ?? Array.Empty<ProjectAutomationRuleResponse>(), correlationId));
    }

    [HttpPost("{projectId:guid}/automation-rules")]
    [SwaggerOperation(Summary = "Create automation rule", Description = "Creates an automation rule. Requires Project.Edit and project member. Triggers: TaskCompleted, TaskOverdue, TaskAssigned. Actions: NotifyUser, ChangeStatus, AddLabel.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectAutomationRuleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectAutomationRuleResponse>>> CreateAutomationRule([FromRoute] Guid projectId, [FromBody] CreateProjectAutomationRuleRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectAutomationRuleResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _automationRuleService.CreateAsync(projectId, request ?? new CreateProjectAutomationRuleRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectAutomationRuleResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectAutomationRuleResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectAutomationRuleResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectAutomationRuleResponse>.Ok(data!, correlationId));
    }

    [HttpDelete("{projectId:guid}/automation-rules/{ruleId:guid}")]
    [SwaggerOperation(Summary = "Delete automation rule", Description = "Deletes an automation rule. Requires Project.Edit and project member.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAutomationRule([FromRoute] Guid projectId, [FromRoute] Guid ruleId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, error) = await _automationRuleService.DeleteAsync(projectId, ruleId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
        }
        return NoContent();
    }

    [HttpGet("{projectId:guid}/export")]
    [SwaggerOperation(Summary = "Export project to CSV", Description = "Returns a CSV file with tasks (Title, Status, Priority, Assignee, Due Date, Labels, Logged Hours). Requires project access.")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (csvBytes, error) = await _exportService.ExportToCsvAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (csvBytes == null)
        {
            if (error?.Contains("not found") == true)
                return NotFound();
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var fileName = $"project-{projectId:N}.csv";
        return File(csvBytes, "text/csv", fileName);
    }

    [HttpGet("{projectId:guid}/custom-fields")]
    [SwaggerOperation(Summary = "List project custom fields", Description = "Returns custom field definitions for the project. Requires project access.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>>> GetCustomFields([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _customFieldService.GetByProjectIdAsync(projectId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
        }
        return Ok(ApiResponse<IReadOnlyList<ProjectCustomFieldResponse>>.Ok(data ?? Array.Empty<ProjectCustomFieldResponse>(), correlationId));
    }

    [HttpPost("{projectId:guid}/custom-fields")]
    [SwaggerOperation(Summary = "Create custom field", Description = "Creates a custom field for the project. Requires Project.Edit and project member.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectCustomFieldResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectCustomFieldResponse>>> CreateCustomField([FromRoute] Guid projectId, [FromBody] CreateProjectCustomFieldRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _customFieldService.CreateAsync(projectId, request ?? new CreateProjectCustomFieldRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectCustomFieldResponse>.Ok(data!, correlationId));
    }

    [HttpPut("{projectId:guid}/custom-fields/{id:guid}")]
    [SwaggerOperation(Summary = "Update custom field", Description = "Updates a custom field. Requires Project.Edit and project member.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectCustomFieldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectCustomFieldResponse>>> UpdateCustomField([FromRoute] Guid projectId, [FromRoute] Guid id, [FromBody] UpdateProjectCustomFieldRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _customFieldService.UpdateAsync(projectId, id, request ?? new UpdateProjectCustomFieldRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectCustomFieldResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return Ok(ApiResponse<ProjectCustomFieldResponse>.Ok(data!, correlationId));
    }

    [HttpDelete("{projectId:guid}/custom-fields/{id:guid}")]
    [SwaggerOperation(Summary = "Delete custom field", Description = "Deletes a custom field and all its task values. Requires Project.Edit and project member.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomField([FromRoute] Guid projectId, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, error) = await _customFieldService.DeleteAsync(projectId, id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true || error?.Contains("required") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
        }
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return null;
        return userId;
    }
}

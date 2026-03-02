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
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List projects", Description = "Returns all projects. Requires Project.View. Returns 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _projectService.GetAllAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);
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

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return null;
        return userId;
    }
}

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
[Route("api/project-templates")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("Project templates. List: Project.Create or Admin. Create/Delete: Admin only.")]
public class ProjectTemplatesController : ControllerBase
{
    private readonly IProjectTemplateService _templateService;
    private readonly ILogger<ProjectTemplatesController> _logger;

    public ProjectTemplatesController(IProjectTemplateService templateService, ILogger<ProjectTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List templates", Description = "Returns all project templates. Requires Project.Create or Admin. Used for 'Create from template' dropdown.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectTemplateResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectTemplateResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<ProjectTemplateResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _templateService.GetAllAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<ProjectTemplateResponse>>.Ok(list, correlationId));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get template by id", Description = "Admin only. Returns a single template with its tasks (for editing).")]
    [ProducesResponseType(typeof(ApiResponse<ProjectTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectTemplateResponse>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var template = await _templateService.GetByIdAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (template == null)
            return NotFound(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = "Template not found or access denied." }, correlationId));
        return Ok(ApiResponse<ProjectTemplateResponse>.Ok(template, correlationId));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create template", Description = "Admin only. Creates a new project template with optional tasks.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectTemplateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ProjectTemplateResponse>>> Create([FromBody] CreateProjectTemplateRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _templateService.CreateAsync(request ?? new CreateProjectTemplateRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("permission") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectTemplateResponse>.Ok(data!, correlationId));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update template", Description = "Admin only. Updates name, description, and replaces all template tasks.")]
    [ProducesResponseType(typeof(ApiResponse<ProjectTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectTemplateResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateProjectTemplateRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _templateService.UpdateAsync(id, request ?? new UpdateProjectTemplateRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<ProjectTemplateResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return Ok(ApiResponse<ProjectTemplateResponse>.Ok(data!, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete template", Description = "Admin only. Deletes a project template and its template tasks.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, error) = await _templateService.DeleteAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true)
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

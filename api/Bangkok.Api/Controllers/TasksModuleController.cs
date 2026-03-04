using System.Security.Claims;
using Bangkok.Application.Dto.TasksStandalone;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/tasks-module")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("Standalone Tasks module. Tenant-scoped; requires module activation and user access. Permissions: Tasks.View, Tasks.Create, Tasks.Edit, Tasks.Delete, Tasks.Assign.")]
public class TasksModuleController : ControllerBase
{
    private readonly ITasksStandaloneService _service;
    private readonly ILogger<TasksModuleController> _logger;

    public TasksModuleController(ITasksStandaloneService service, ILogger<TasksModuleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List tasks", Description = "Returns standalone tasks for the tenant. Supports filters: status, assignedToUserId, priority, dueBefore, search. Requires Tasks.View.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TasksStandaloneResponse>>>> GetList(
        [FromQuery] string? status,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] string? priority,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var filter = new TasksStandaloneFilterRequest
        {
            Status = status,
            AssignedToUserId = assignedToUserId,
            Priority = priority,
            DueBefore = dueBefore,
            Search = search
        };
        var (success, data, error) = await _service.GetListAsync(userId.Value, filter, myTasksOnly: false, cancellationToken).ConfigureAwait(false);
        if (!success)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        return Ok(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Ok(data ?? Array.Empty<TasksStandaloneResponse>(), correlationId));
    }

    [HttpGet("my")]
    [SwaggerOperation(Summary = "My tasks", Description = "Returns tasks assigned to the current user. Requires Tasks.View.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TasksStandaloneResponse>>>> GetMyTasks(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var filter = new TasksStandaloneFilterRequest { Status = status, Priority = priority, DueBefore = dueBefore, Search = search };
        var (success, data, error) = await _service.GetListAsync(userId.Value, filter, myTasksOnly: true, cancellationToken).ConfigureAwait(false);
        if (!success)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        return Ok(ApiResponse<IReadOnlyList<TasksStandaloneResponse>>.Ok(data ?? Array.Empty<TasksStandaloneResponse>(), correlationId));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get task by ID", Description = "Returns a single standalone task. Requires Tasks.View.")]
    [ProducesResponseType(typeof(ApiResponse<TasksStandaloneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TasksStandaloneResponse>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _service.GetByIdAsync(id, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        if (data == null)
            return NotFound(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = "Task not found." }, correlationId));
        return Ok(ApiResponse<TasksStandaloneResponse>.Ok(data, correlationId));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create task", Description = "Creates a new standalone task. Requires Tasks.Create; Tasks.Assign if assigning. Respects subscription task limit.")]
    [ProducesResponseType(typeof(ApiResponse<TasksStandaloneResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TasksStandaloneResponse>>> Create([FromBody] CreateTasksStandaloneRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _service.CreateAsync(request ?? new CreateTasksStandaloneRequest(), userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("limit") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "LIMIT_EXCEEDED", Message = error }, correlationId));
            if (error?.Contains("required") == true || error?.Contains("Title") == true)
                return BadRequest(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        }
        if (data == null)
            return BadRequest(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Create failed." }, correlationId));
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<TasksStandaloneResponse>.Ok(data, correlationId));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update task", Description = "Updates a standalone task. Requires Tasks.Edit; Tasks.Assign to change assignee. Completed tasks only allow reopening (status to Open).")]
    [ProducesResponseType(typeof(ApiResponse<TasksStandaloneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TasksStandaloneResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateTasksStandaloneRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _service.UpdateAsync(id, request ?? new UpdateTasksStandaloneRequest(), userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        if (data == null)
            return BadRequest(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Update failed." }, correlationId));
        return Ok(ApiResponse<TasksStandaloneResponse>.Ok(data, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete task", Description = "Deletes a standalone task. Requires Tasks.Delete.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _service.DeleteAsync(id, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        }
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(Summary = "Set status", Description = "Sets task status to Open or Completed (complete/reopen). Requires Tasks.Edit.")]
    [ProducesResponseType(typeof(ApiResponse<TasksStandaloneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TasksStandaloneResponse>>> SetStatus([FromRoute] Guid id, [FromQuery] string status, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _service.SetStatusAsync(id, status ?? "", userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            return BadRequest(ApiResponse<TasksStandaloneResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid status." }, correlationId));
        }
        return Ok(ApiResponse<TasksStandaloneResponse>.Ok(data!, correlationId));
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }
}

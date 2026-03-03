using System.Security.Claims;
using Bangkok.Application.Dto.Tasks;
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
[SwaggerTag("Task planning. All endpoints require Bearer token. Permission-based: Task.View, Task.Create, Task.Edit, Task.Delete, Task.Assign. Returns 403 when permission missing.")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskCommentService _commentService;
    private readonly ITaskActivityService _activityService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ITaskCommentService commentService, ITaskActivityService activityService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _commentService = commentService;
        _activityService = activityService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List tasks by project", Description = "Returns tasks for the given projectId with optional filters. Query: projectId (required); status, priority, assignedToUserId, labelId, dueBefore, dueAfter, search. Requires Task.View. 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskResponse>>>> GetByProjectId(
        [FromQuery] Guid projectId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] Guid? labelId,
        [FromQuery] DateTime? dueBefore,
        [FromQuery] DateTime? dueAfter,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TaskResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        if (projectId == Guid.Empty)
            return BadRequest(ApiResponse<IReadOnlyList<TaskResponse>>.Fail(new ErrorResponse { Code = "VALIDATION", Message = "projectId query parameter is required." }, correlationId));

        TaskFilterRequest? filter = null;
        if (!string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(priority) || (assignedToUserId.HasValue && assignedToUserId.Value != Guid.Empty) ||
            (labelId.HasValue && labelId.Value != Guid.Empty) || dueBefore.HasValue || dueAfter.HasValue || !string.IsNullOrWhiteSpace(search))
        {
            filter = new TaskFilterRequest
            {
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                Priority = string.IsNullOrWhiteSpace(priority) ? null : priority,
                AssignedToUserId = assignedToUserId.HasValue && assignedToUserId.Value != Guid.Empty ? assignedToUserId : null,
                LabelId = labelId.HasValue && labelId.Value != Guid.Empty ? labelId : null,
                DueBefore = dueBefore,
                DueAfter = dueAfter,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
            };
        }

        var list = await _taskService.GetByProjectIdAsync(projectId, currentUserId.Value, filter, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<TaskResponse>>.Ok(list, correlationId));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get task by ID", Description = "Returns a single task. Requires Task.View. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, data) = await _taskService.GetByIdAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == GetTaskResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have permission to view tasks." }, correlationId));
        if (result == GetTaskResult.NotFound || data == null)
            return NotFound(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "TASK_NOT_FOUND", Message = "Task not found." }, correlationId));

        return Ok(ApiResponse<TaskResponse>.Ok(data, correlationId));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create task", Description = "Creates a new task. Requires Task.Create. Project must exist. Returns 201 with created resource. 403 if permission missing, 400 if project not found or invalid.")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, data, errorMessage) = await _taskService.CreateAsync(request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == CreateTaskResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to create tasks." }, correlationId));
        if (result == CreateTaskResult.ProjectNotFound)
            return BadRequest(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "PROJECT_NOT_FOUND", Message = errorMessage ?? "Project not found." }, correlationId));
        if (result == CreateTaskResult.ValidationError)
            return BadRequest(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = errorMessage ?? "Invalid request." }, correlationId));
        if (data == null)
            return BadRequest(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "ERROR", Message = "Create failed." }, correlationId));

        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<TaskResponse>.Ok(data, correlationId));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update task", Description = "Updates an existing task. Requires Task.Edit. Assigning a user requires Task.Assign. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (result, errorMessage) = await _taskService.UpdateAsync(id, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == UpdateTaskResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to edit tasks." }, correlationId));
        if (result == UpdateTaskResult.AssignForbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = errorMessage ?? "You do not have permission to assign tasks." }, correlationId));
        if (result == UpdateTaskResult.NotFound)
            return NotFound(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "TASK_NOT_FOUND", Message = "Task not found." }, correlationId));
        if (result == UpdateTaskResult.ValidationError)
            return BadRequest(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = errorMessage ?? "Invalid request." }, correlationId));

        var updated = await _taskService.GetByIdAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<TaskResponse>.Ok(updated.Data!, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete task", Description = "Deletes a task. Requires Task.Delete. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var result = await _taskService.DeleteAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (result == DeleteTaskResult.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have permission to delete tasks." }, correlationId));
        if (result == DeleteTaskResult.NotFound)
            return NotFound(ApiResponse<TaskResponse>.Fail(new ErrorResponse { Code = "TASK_NOT_FOUND", Message = "Task not found." }, correlationId));

        return NoContent();
    }

    [HttpGet("{taskId:guid}/comments")]
    [SwaggerOperation(Summary = "List task comments", Description = "Returns comments for the task. Requires Task.View. 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskCommentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskCommentResponse>>>> GetComments([FromRoute] Guid taskId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TaskCommentResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _commentService.GetByTaskIdAsync(taskId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<TaskCommentResponse>>.Ok(list, correlationId));
    }

    [HttpPost("{taskId:guid}/comments")]
    [SwaggerOperation(Summary = "Add task comment", Description = "Adds a comment to the task. Requires Task.Comment. 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<TaskCommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TaskCommentResponse>>> CreateComment([FromRoute] Guid taskId, [FromBody] CreateTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TaskCommentResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, data, error) = await _commentService.CreateAsync(taskId, request, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<TaskCommentResponse>.Fail(new ErrorResponse { Code = "TASK_NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("permission") == true)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<TaskCommentResponse>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error }, correlationId));
            return BadRequest(ApiResponse<TaskCommentResponse>.Fail(new ErrorResponse { Code = "VALIDATION", Message = error ?? "Invalid request." }, correlationId));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TaskCommentResponse>.Ok(data!, correlationId));
    }

    [HttpGet("{taskId:guid}/activities")]
    [SwaggerOperation(Summary = "List task activities", Description = "Returns activity log for the task. Requires Task.ViewActivity. 403 if permission missing.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskActivityResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskActivityResponse>>>> GetActivities([FromRoute] Guid taskId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TaskActivityResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _activityService.GetByTaskIdAsync(taskId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<TaskActivityResponse>>.Ok(list, correlationId));
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return null;
        return userId;
    }
}

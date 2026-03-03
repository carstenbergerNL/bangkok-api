using System.Security.Claims;
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
[SwaggerTag("Time log entries. DELETE by id. Requires Task.Edit and project membership.")]
public class TimelogsController : ControllerBase
{
    private readonly ITaskTimeLogService _timeLogService;
    private readonly ILogger<TimelogsController> _logger;

    public TimelogsController(ITaskTimeLogService timeLogService, ILogger<TimelogsController> logger)
    {
        _timeLogService = timeLogService;
        _logger = logger;
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete time log", Description = "Deletes a time log entry. Requires Task.Edit and project membership. 403 if permission missing, 404 if not found.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _timeLogService.DeleteAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "TIMELOG_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
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

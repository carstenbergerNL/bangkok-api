using System.Security.Claims;
using Bangkok.Application.Dto.Notifications;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/Notifications")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("In-app notifications. All endpoints require Bearer token. Users see only their own notifications.")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List notifications", Description = "Returns the current user's notifications (newest first).")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationResponse>>>> Get(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<NotificationResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var list = await _notificationService.GetByUserIdAsync(userId.Value, 50, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<NotificationResponse>>.Ok(list, correlationId));
    }

    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Unread count", Description = "Returns the number of unread notifications for the current user.")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<int>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var count = await _notificationService.GetUnreadCountAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<int>.Ok(count, correlationId));
    }

    [HttpPut("{id:guid}/read")]
    [SwaggerOperation(Summary = "Mark as read", Description = "Marks a single notification as read. Only the owner can mark.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, error) = await _notificationService.MarkReadAsync(id, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOTIFICATION_NOT_FOUND", Message = error }, correlationId));
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        }
        return Ok(ApiResponse<object>.Ok(null, correlationId));
    }

    [HttpPut("read-all")]
    [SwaggerOperation(Summary = "Mark all as read", Description = "Marks all notifications for the current user as read.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        await _notificationService.MarkAllReadAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<object>.Ok(null, correlationId));
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return null;
        return userId;
    }
}

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
[SwaggerTag("Task file attachments. List/upload on Tasks controller. Download and delete here. Task.View to list/download, Task.Edit to upload, Admin or uploader to delete.")]
public class AttachmentsController : ControllerBase
{
    private readonly ITaskAttachmentService _attachmentService;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(ITaskAttachmentService attachmentService, ILogger<AttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _logger = logger;
    }

    [HttpGet("{id:guid}/download")]
    [SwaggerOperation(Summary = "Download attachment", Description = "Streams the file. Requires Task.View on the task. Returns 404 if attachment or file missing.")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, content, fileName, contentType, error) = await _attachmentService.GetDownloadStreamAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true || error?.Contains("Access denied") == true)
                return NotFound();
            if (error?.Contains("permission") == true)
                return StatusCode(StatusCodes.Status403Forbidden);
            return NotFound();
        }
        if (content == null)
            return NotFound();
        var name = fileName ?? "attachment";
        var ct = contentType ?? "application/octet-stream";
        return File(content, ct, name);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete attachment", Description = "Admin or uploader can delete. Requires Task.View; only uploader or Admin can delete. Removes file from storage and DB.")]
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

        var (success, error) = await _attachmentService.DeleteAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            if (error?.Contains("denied") == true || error?.Contains("uploader") == true)
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

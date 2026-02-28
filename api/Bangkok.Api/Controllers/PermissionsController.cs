using Bangkok.Application.Dto.Permissions;
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
[SwaggerTag("Permission management. Admin only.")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "List permissions", Description = "Get all permissions. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> GetPermissions(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var permissions = await _permissionService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(permissions, correlationId));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Get permission by ID", Description = "Get a single permission. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> GetPermission([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var permission = await _permissionService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return NotFound(ApiResponse<PermissionResponse>.Fail(new ErrorResponse { Code = "PERMISSION_NOT_FOUND", Message = "Permission not found." }, correlationId));
        return Ok(ApiResponse<PermissionResponse>.Ok(permission, correlationId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Create permission", Description = "Create a new permission. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<PermissionResponse>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Permission name is required." }, correlationId));

        var permission = await _permissionService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return BadRequest(ApiResponse<PermissionResponse>.Fail(new ErrorResponse { Code = "PERMISSION_EXISTS", Message = "A permission with this name already exists." }, correlationId));

        _logger.LogInformation("Permission created. PermissionId: {PermissionId}, Name: {Name}", permission.Id, permission.Name);
        return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, ApiResponse<PermissionResponse>.Ok(permission, correlationId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Update permission", Description = "Update a permission. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> UpdatePermission([FromRoute] Guid id, [FromBody] UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request == null)
            return BadRequest(ApiResponse<PermissionResponse>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Request body is required." }, correlationId));

        var permission = await _permissionService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (permission == null)
            return NotFound(ApiResponse<PermissionResponse>.Fail(new ErrorResponse { Code = "PERMISSION_NOT_FOUND", Message = "Permission not found or duplicate name." }, correlationId));

        _logger.LogInformation("Permission updated. PermissionId: {PermissionId}, Name: {Name}", permission.Id, permission.Name);
        return Ok(ApiResponse<PermissionResponse>.Ok(permission, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Delete permission", Description = "Delete a permission and remove it from all roles. Admin only.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermission([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var deleted = await _permissionService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "PERMISSION_NOT_FOUND", Message = "Permission not found." }, correlationId));

        _logger.LogInformation("Permission deleted. PermissionId: {PermissionId}", id);
        return NoContent();
    }
}

using System.Security.Claims;
using Bangkok.Application.Dto.TenantAdmin;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/tenant-admin")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[Produces("application/json")]
[SwaggerTag("Tenant Admin: manage users and module access within the current tenant. Tenant Admin or Platform Admin only.")]
public class TenantAdminController : ControllerBase
{
    private readonly ITenantAdminService _tenantAdminService;
    private readonly ILogger<TenantAdminController> _logger;

    public TenantAdminController(ITenantAdminService tenantAdminService, ILogger<TenantAdminController> logger)
    {
        _tenantAdminService = tenantAdminService;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantAdminUserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "List tenant users", Description = "Returns all users in the current tenant with role and module access. Tenant Admin or Platform Admin only.")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantAdminUserResponse>>>> GetUsers(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<IReadOnlyList<TenantAdminUserResponse>>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (allowed, users, error) = await _tenantAdminService.GetUsersAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!allowed)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<TenantAdminUserResponse>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        return Ok(ApiResponse<IReadOnlyList<TenantAdminUserResponse>>.Ok(users ?? Array.Empty<TenantAdminUserResponse>(), correlationId));
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResponse<TenantAdminUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Invite or add user", Description = "Add existing user by email to tenant, or create new user and add. Assign role and module access. Tenant Admin or Platform Admin only.")]
    public async Task<ActionResult<ApiResponse<TenantAdminUserResponse>>> InviteOrAddUser([FromBody] InviteTenantUserRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<TenantAdminUserResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var (success, user, error) = await _tenantAdminService.InviteOrAddUserAsync(request ?? new InviteTenantUserRequest(), currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
            return BadRequest(ApiResponse<TenantAdminUserResponse>.Fail(new ErrorResponse { Code = "INVITE_FAILED", Message = error ?? "Failed to add user." }, correlationId));
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TenantAdminUserResponse>.Ok(user!, correlationId));
    }

    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Remove user from tenant", Description = "Removes user from current tenant. Cannot remove the last Tenant Admin. Tenant Admin or Platform Admin only.")]
    public async Task<IActionResult> RemoveUser([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, error) = await _tenantAdminService.RemoveUserAsync(userId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "REMOVE_FAILED", Message = error ?? "Failed to remove user." }, correlationId));
        }
        return NoContent();
    }

    [HttpPut("users/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Update user role", Description = "Set tenant role to Admin or Member. Cannot demote the last Tenant Admin. Tenant Admin or Platform Admin only.")]
    public async Task<IActionResult> UpdateUserRole([FromRoute] Guid userId, [FromBody] UpdateTenantUserRoleRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var role = request?.Role?.Trim() ?? "Member";
        var (success, error) = await _tenantAdminService.UpdateUserRoleAsync(userId, role, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "ROLE_UPDATE_FAILED", Message = error ?? "Failed to update role." }, correlationId));
        }
        return NoContent();
    }

    [HttpPut("users/{userId:guid}/modules")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Update user module access", Description = "Replace user's module access with the given list. Only active tenant modules are applied. Tenant Admin or Platform Admin only.")]
    public async Task<IActionResult> UpdateUserModules([FromRoute] Guid userId, [FromBody] UpdateTenantUserModulesRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var moduleKeys = request?.ModuleKeys ?? Array.Empty<string>();
        var (success, error) = await _tenantAdminService.UpdateUserModulesAsync(userId, moduleKeys, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "NOT_FOUND", Message = error }, correlationId));
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "MODULES_UPDATE_FAILED", Message = error ?? "Failed to update modules." }, correlationId));
        }
        return NoContent();
    }
}

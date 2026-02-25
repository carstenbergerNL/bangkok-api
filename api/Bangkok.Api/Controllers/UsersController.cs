using System.Security.Claims;
using Bangkok.Application.Dto.Users;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>Get a single user by ID. Returns safe fields only. Caller must be the user or Admin.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized();

        if (id != currentUserId.Value && !IsAdmin(currentUserRole))
        {
            _logger.LogWarning("Unauthorized get user attempt: user {CurrentUserId} tried to get user {TargetUserId}", currentUserId, id);
            return Forbid();
        }

        var user = await _userService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return NotFound(ApiResponse<UserResponse>.Fail(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = "User not found."
            }, correlationId));

        return Ok(ApiResponse<UserResponse>.Ok(user, correlationId));
    }

    /// <summary>Get paginated list of users. Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _userService.GetUsersAsync(pageNumber, pageSize, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result, correlationId));
    }

    /// <summary>Update user profile. Users can update own email only; Admin can update Email, Role, IsActive.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized();

        var result = await _userService.UpdateUserAsync(id, request ?? new UpdateUserRequest(), currentUserId.Value, currentUserRole ?? string.Empty, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            UpdateUserResult.NotFound => NotFound(ApiResponse<UserResponse>.Fail(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = "User not found."
            }, correlationId)),
            UpdateUserResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<UserResponse>.Fail(new ErrorResponse
            {
                Code = "FORBIDDEN",
                Message = "You are not allowed to perform this action."
            }, correlationId)),
            UpdateUserResult.BadRequest => BadRequest(ApiResponse<UserResponse>.Fail(new ErrorResponse
            {
                Code = "BAD_REQUEST",
                Message = "Invalid request (e.g. email already in use or no allowed fields provided)."
            }, correlationId)),
            _ => Ok(ApiResponse<UserResponse>.Ok((await _userService.GetUserAsync(id, cancellationToken).ConfigureAwait(false))!, correlationId))
        };
    }

    /// <summary>Soft-delete a user. Admin only. Returns 204 on success. Cannot delete yourself.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var (currentUserId, _) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized();

        var result = await _userService.DeleteUserAsync(id, currentUserId.Value, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            DeleteUserResult.Success => NoContent(),
            DeleteUserResult.NotFound => NotFound(),
            DeleteUserResult.AlreadyDeleted => BadRequest(),
            DeleteUserResult.ForbiddenSelfDelete => BadRequest(),
            _ => NoContent()
        };
    }

    /// <summary>Dangerous operation: Permanently delete a user and their refresh tokens. Admin only. Requires query parameter confirm=true. Cannot delete yourself. Returns 204 on success.</summary>
    [HttpDelete("{id:guid}/hard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HardDeleteUser(
        [FromRoute] Guid id,
        [FromQuery] bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        var (currentUserId, _) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized();

        var result = await _userService.HardDeleteUserAsync(id, currentUserId.Value, confirm, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            HardDeleteUserResult.Success => NoContent(),
            HardDeleteUserResult.NotFound => NotFound(),
            HardDeleteUserResult.ForbiddenSelfDelete => BadRequest(),
            HardDeleteUserResult.ConfirmRequired => BadRequest(),
            _ => NoContent()
        };
    }

    /// <summary>Restore a soft-deleted user. Admin only. Returns 204 on success.</summary>
    [HttpPatch("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RestoreUserAsync(id, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            RestoreUserResult.Success => NoContent(),
            RestoreUserResult.NotFound => NotFound(),
            RestoreUserResult.NotDeleted => BadRequest(),
            _ => NoContent()
        };
    }

    private (Guid? UserId, string? Role) GetCurrentUserIdentity()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return (null, roleClaim);
        return (userId, roleClaim);
    }

    private static bool IsAdmin(string? role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
}

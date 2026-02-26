using System.Security.Claims;
using Bangkok.Application.Dto.Profile;
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
[SwaggerTag("User profile (1:1 with User). All endpoints require Bearer token. Users can access own profile only unless Admin.")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet("{userId:guid}")]
    [SwaggerOperation(Summary = "Get profile by user ID", Description = "Returns the profile for the given user. Caller must be the user or Admin.")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetByUserId(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        if (userId != currentUserId.Value && !IsAdmin(currentUserRole))
        {
            _logger.LogWarning("Unauthorized get profile: user {CurrentUserId} tried to get profile for user {TargetUserId}", currentUserId, userId);
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have access to this resource." }, correlationId));
        }

        var profile = await _profileService.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile == null)
            return NotFound(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "PROFILE_NOT_FOUND", Message = "Profile not found." }, correlationId));

        return Ok(ApiResponse<ProfileDto>.Ok(profile, correlationId));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create profile", Description = "Creates a profile for the given user (1:1). Caller must be the user or Admin. Avatar: base64, max 2MB, jpeg/png.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Create(
        [FromBody] CreateProfileDto dto,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        if (dto == null)
            return BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Request body is required." }, correlationId));
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Keys.SelectMany(key => ModelState[key]!.Errors.Select(e => new ApiError { Code = "VALIDATION", Message = e.ErrorMessage ?? "Invalid value." })).ToList();
            return BadRequest(ApiResponse<ProfileDto>.Fail(errors, correlationId));
        }

        var (result, errorMessage) = await _profileService.CreateProfileAsync(dto, currentUserId.Value, currentUserRole, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            CreateProfileResult.Success => StatusCode(StatusCodes.Status201Created, ApiResponse<ProfileDto>.Ok(
                (await _profileService.GetByUserIdAsync(dto.UserId, cancellationToken).ConfigureAwait(false))!, correlationId, "Profile created.")),
            CreateProfileResult.NotFound => NotFound(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "USER_NOT_FOUND", Message = errorMessage ?? "User not found." }, correlationId)),
            CreateProfileResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have access to this resource." }, correlationId)),
            CreateProfileResult.AlreadyExists => StatusCode(StatusCodes.Status409Conflict, ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "PROFILE_ALREADY_EXISTS", Message = errorMessage ?? "Profile already exists for this user." }, correlationId)),
            CreateProfileResult.ValidationError => BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "VALIDATION_ERROR", Message = errorMessage ?? "Validation failed." }, correlationId)),
            _ => BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = errorMessage ?? "Request failed." }, correlationId))
        };
    }

    [HttpPut("{userId:guid}")]
    [SwaggerOperation(Summary = "Update profile", Description = "Updates the profile for the given user. Caller must be the user or Admin. Send only fields to update; avatar base64 max 2MB, jpeg/png.")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Update(
        [FromRoute] Guid userId,
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        if (dto == null)
            return BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Request body is required." }, correlationId));
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Keys.SelectMany(key => ModelState[key]!.Errors.Select(e => new ApiError { Code = "VALIDATION", Message = e.ErrorMessage ?? "Invalid value." })).ToList();
            return BadRequest(ApiResponse<ProfileDto>.Fail(errors, correlationId));
        }

        var (result, errorMessage) = await _profileService.UpdateProfileAsync(userId, dto, currentUserId.Value, currentUserRole, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            UpdateProfileResult.Success => Ok(ApiResponse<ProfileDto>.Ok(
                (await _profileService.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false))!, correlationId)),
            UpdateProfileResult.NotFound => NotFound(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "PROFILE_NOT_FOUND", Message = "Profile not found." }, correlationId)),
            UpdateProfileResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have access to this resource." }, correlationId)),
            UpdateProfileResult.ValidationError => BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "VALIDATION_ERROR", Message = errorMessage ?? "Validation failed." }, correlationId)),
            _ => BadRequest(ApiResponse<ProfileDto>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = errorMessage ?? "Request failed." }, correlationId))
        };
    }

    [HttpDelete("{userId:guid}")]
    [SwaggerOperation(Summary = "Delete profile", Description = "Deletes the profile for the given user. Caller must be the user or Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var (currentUserId, currentUserRole) = GetCurrentUserIdentity();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, correlationId));

        var result = await _profileService.DeleteProfileAsync(userId, currentUserId.Value, currentUserRole, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            DeleteProfileResult.Success => NoContent(),
            DeleteProfileResult.NotFound => NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "PROFILE_NOT_FOUND", Message = "Profile not found." }, correlationId)),
            DeleteProfileResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "You do not have access to this resource." }, correlationId)),
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

using Bangkok.Application.Dto.Auth;
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
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("Authentication: register, login, refresh, revoke, password recovery.")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("RegisterPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var response = await _authService.RegisterAsync(request, cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            _logger.LogWarning("Registration failed: email already exists. Email: {Email}", request.Email);
            return BadRequest(ApiResponse<AuthResponse>.Fail(new ErrorResponse { Code = "REGISTRATION_FAILED", Message = "A user with this email already exists." }, correlationId));
        }
        _logger.LogInformation("User registered successfully. Email: {Email}", request.Email);
        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Ok(response, correlationId));
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [SwaggerOperation(Summary = "Login", Description = "Authenticate with email and password. Returns access and refresh tokens. After 5 failed attempts the account is locked for 15 minutes (403). Rate limited.")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, cancellationToken, clientIp).ConfigureAwait(false);
        if (result.IsLocked)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AuthResponse>.Fail(new ErrorResponse { Code = "ACCOUNT_LOCKED", Message = "Account temporarily locked." }, correlationId));
        if (!result.Success)
            return Unauthorized(ApiResponse<AuthResponse>.Fail(new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid email or password." }, correlationId));
        return Ok(ApiResponse<AuthResponse>.Ok(result.AuthResponse!, correlationId));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var response = await _authService.RefreshAsync(request, cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            _logger.LogWarning("Refresh token invalid or expired. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(ApiResponse<AuthResponse>.Fail(new ErrorResponse { Code = "INVALID_REFRESH_TOKEN", Message = "Refresh token is invalid or has expired." }, correlationId));
        }
        return Ok(ApiResponse<AuthResponse>.Ok(response, correlationId));
    }

    [HttpPost("revoke")]
    [Authorize]
    [SwaggerOperation(Summary = "Revoke refresh token", Description = "Revoke a refresh token. Requires Bearer token.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var revoked = await _authService.RevokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (!revoked)
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "REVOKE_FAILED", Message = "Refresh token not found or already revoked." }, correlationId));
        _logger.LogInformation("Refresh token revoked. CorrelationId: {CorrelationId}", correlationId);
        return Ok(ApiResponse<object>.Ok(null!, correlationId));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPasswordPolicy")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        await _authService.ForgotPasswordAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(new ForgotPasswordResponse(), correlationId));
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(Summary = "Reset password", Description = "Reset password using the recovery string from forgot-password. New password min length 8.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var success = await _authService.ResetPasswordAsync(request, cancellationToken).ConfigureAwait(false);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "INVALID_RECOVERY", Message = "Recovery link is invalid or has expired." }, correlationId));
        return Ok(ApiResponse<object>.Ok(null!, correlationId));
    }
}

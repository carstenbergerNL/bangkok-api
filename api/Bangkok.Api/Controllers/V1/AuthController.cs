using Asp.Versioning;
using Bangkok.Application.Dto.Auth;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bangkok.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user and return access and refresh tokens.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var response = await _authService.RegisterAsync(request, cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            _logger.LogWarning("Registration failed: email already exists. Email: {Email}", request.Email);
            return BadRequest(ApiResponse<AuthResponse>.Fail(new ErrorResponse
            {
                Code = "REGISTRATION_FAILED",
                Message = "A user with this email already exists."
            }, correlationId));
        }
        _logger.LogInformation("User registered successfully. Email: {Email}", request.Email);
        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Ok(response, correlationId));
    }

    /// <summary>Authenticate and return access and refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var response = await _authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(ApiResponse<AuthResponse>.Fail(new ErrorResponse
            {
                Code = "INVALID_CREDENTIALS",
                Message = "Invalid email or password."
            }, correlationId));
        }
        _logger.LogInformation("User logged in. Email: {Email}", request.Email);
        return Ok(ApiResponse<AuthResponse>.Ok(response, correlationId));
    }

    /// <summary>Exchange a valid refresh token for new access and refresh tokens.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var response = await _authService.RefreshAsync(request, cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            _logger.LogWarning("Refresh token invalid or expired. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(ApiResponse<AuthResponse>.Fail(new ErrorResponse
            {
                Code = "INVALID_REFRESH_TOKEN",
                Message = "Refresh token is invalid or has expired."
            }, correlationId));
        }
        return Ok(ApiResponse<AuthResponse>.Ok(response, correlationId));
    }

    /// <summary>Revoke a refresh token.</summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(
        [FromBody] RevokeRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var revoked = await _authService.RevokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (!revoked)
        {
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse
            {
                Code = "REVOKE_FAILED",
                Message = "Refresh token not found or already revoked."
            }, correlationId));
        }
        _logger.LogInformation("Refresh token revoked. CorrelationId: {CorrelationId}", correlationId);
        return Ok(ApiResponse<object>.Ok(null!, correlationId));
    }

    /// <summary>Request password recovery. Always returns success to avoid revealing whether the email exists.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        await _authService.ForgotPasswordAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(new ForgotPasswordResponse(), correlationId));
    }

    /// <summary>Reset password using a valid recovery string from forgot-password.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var success = await _authService.ResetPasswordAsync(request, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse
            {
                Code = "INVALID_RECOVERY",
                Message = "Recovery link is invalid or has expired."
            }, correlationId));
        }
        return Ok(ApiResponse<object>.Ok(null!, correlationId));
    }
}

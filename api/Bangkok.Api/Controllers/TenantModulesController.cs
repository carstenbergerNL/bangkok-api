using System.Security.Claims;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/tenant/modules")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[Produces("application/json")]
[SwaggerTag("Tenant module activation: list active modules, manage modules (admin), user-level access (admin).")]
public class TenantModulesController : ControllerBase
{
    private const string AdminRole = "Admin";

    private readonly ITenantModuleService _tenantModuleService;
    private readonly ITenantModuleUserService _tenantModuleUserService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantModulesController> _logger;

    public TenantModulesController(
        ITenantModuleService tenantModuleService,
        ITenantModuleUserService tenantModuleUserService,
        ITenantContext tenantContext,
        ILogger<TenantModulesController> logger)
    {
        _tenantModuleService = tenantModuleService;
        _tenantModuleUserService = tenantModuleUserService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    /// <summary>
    /// Get module keys the current user can access (active for tenant and user has access). Used by sidebar.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ActiveModulesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Get active module keys for current user", Description = "Returns module keys the current user can access. Used by sidebar.")]
    public async Task<ActionResult<ApiResponse<ActiveModulesResponse>>> GetActiveModules(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            var cid = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
            return Unauthorized(ApiResponse<ActiveModulesResponse>.Fail(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required." }, cid));
        }
        var keys = await _tenantModuleService.GetActiveModuleKeysForUserAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        return Ok(ApiResponse<ActiveModulesResponse>.Ok(new ActiveModulesResponse { ActiveModuleKeys = keys.ToList() }, correlationId));
    }

    /// <summary>
    /// Get all modules with active flag for current tenant. Admin only. For "Manage Modules" UI.
    /// </summary>
    [HttpGet("management")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "List tenant modules (Admin)", Description = "All modules with IsActive for current tenant. Platform admin only.")]
    public async Task<ActionResult<ApiResponse<object>>> GetModulesManagement(CancellationToken cancellationToken)
    {
        var list = await _tenantModuleService.GetTenantModulesAsync(cancellationToken).ConfigureAwait(false);
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        return Ok(ApiResponse<object>.Ok(list, correlationId));
    }

    /// <summary>
    /// Enable or disable a module for the current tenant. Admin only.
    /// </summary>
    [HttpPut("{moduleKey}/active")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Set module active (Admin)", Description = "Enable or disable a module for the current tenant. Tenant admin only. Body: { isActive: true|false }.")]
    public async Task<ActionResult> SetModuleActive(string moduleKey, [FromBody] SetModuleActiveRequest request, CancellationToken cancellationToken)
    {
        var (success, error) = await _tenantModuleService.SetModuleActiveAsync(moduleKey, request.IsActive, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
            _logger.LogWarning("SetModuleActive failed. ModuleKey: {ModuleKey}, Error: {Error}", moduleKey, error);
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "MODULE_UPDATE_FAILED", Message = error ?? "Failed to update module." }, correlationId));
        }
        return NoContent();
    }

    /// <summary>
    /// List tenant users with their access flag for the module. Tenant Admin only.
    /// </summary>
    [HttpGet("{moduleKey}/users")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ModuleAccessUserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "List users with module access", Description = "Returns tenant users and whether each has access to the module. Tenant Admin only.")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModuleAccessUserDto>>>> GetModuleUsers(string moduleKey, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var tenantId = _tenantContext.CurrentTenantId;
        var userId = GetCurrentUserId();
        if (!tenantId.HasValue || userId == null)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ModuleAccessUserDto>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "Tenant context required." }, correlationId));

        var (allowed, users, error) = await _tenantModuleUserService.GetUsersWithAccessAsync(tenantId.Value, moduleKey, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!allowed)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyList<ModuleAccessUserDto>>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        return Ok(ApiResponse<IReadOnlyList<ModuleAccessUserDto>>.Ok(users ?? Array.Empty<ModuleAccessUserDto>(), correlationId));
    }

    /// <summary>
    /// Grant a user access to the module. Tenant Admin only.
    /// </summary>
    [HttpPost("{moduleKey}/users")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Grant module access", Description = "Grant a tenant user access to the module. Body: { userId }. Tenant Admin only.")]
    public async Task<ActionResult> GrantModuleAccess(string moduleKey, [FromBody] GrantModuleAccessRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var tenantId = _tenantContext.CurrentTenantId;
        var currentUserId = GetCurrentUserId();
        if (!tenantId.HasValue || currentUserId == null)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "Tenant context required." }, correlationId));
        if (request?.UserId == null || request.UserId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "VALIDATION", Message = "UserId is required." }, correlationId));

        var (success, error) = await _tenantModuleUserService.GrantAccessAsync(tenantId.Value, moduleKey, request.UserId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "MODULE_ACCESS_FAILED", Message = error ?? "Failed to grant access." }, correlationId));
        return NoContent();
    }

    /// <summary>
    /// Revoke a user's access to the module. Tenant Admin only.
    /// </summary>
    [HttpDelete("{moduleKey}/users/{userId:guid}")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Revoke module access", Description = "Revoke a tenant user's access to the module. Tenant Admin only.")]
    public async Task<ActionResult> RevokeModuleAccess(string moduleKey, Guid userId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var tenantId = _tenantContext.CurrentTenantId;
        var currentUserId = GetCurrentUserId();
        if (!tenantId.HasValue || currentUserId == null)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = "Tenant context required." }, correlationId));

        var (success, error) = await _tenantModuleUserService.RevokeAccessAsync(tenantId.Value, moduleKey, userId, currentUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!success)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(new ErrorResponse { Code = "FORBIDDEN", Message = error ?? "Access denied." }, correlationId));
        return NoContent();
    }
}

public class ActiveModulesResponse
{
    public List<string> ActiveModuleKeys { get; set; } = new();
}

public class SetModuleActiveRequest
{
    public bool IsActive { get; set; }
}

public class GrantModuleAccessRequest
{
    public Guid UserId { get; set; }
}

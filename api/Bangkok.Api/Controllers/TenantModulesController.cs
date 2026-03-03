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
[SwaggerTag("Tenant module activation: list active modules, manage modules (admin).")]
public class TenantModulesController : ControllerBase
{
    private const string AdminRole = "Admin";

    private readonly ITenantModuleService _tenantModuleService;
    private readonly ILogger<TenantModulesController> _logger;

    public TenantModulesController(ITenantModuleService tenantModuleService, ILogger<TenantModulesController> logger)
    {
        _tenantModuleService = tenantModuleService;
        _logger = logger;
    }

    /// <summary>
    /// Get module keys that are active for the current tenant. Used by frontend (e.g. sidebar) to show only enabled modules.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ActiveModulesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Get active module keys", Description = "Returns the list of module keys enabled for the current tenant. Requires tenant context.")]
    public async Task<ActionResult<ApiResponse<ActiveModulesResponse>>> GetActiveModules(CancellationToken cancellationToken)
    {
        var keys = await _tenantModuleService.GetActiveModuleKeysAsync(cancellationToken).ConfigureAwait(false);
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
    [SwaggerOperation(Summary = "Set module active (Admin)", Description = "Enable or disable a module for the current tenant. Platform admin only. Body: { isActive: true|false }.")]
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
}

public class ActiveModulesResponse
{
    public List<string> ActiveModuleKeys { get; set; } = new();
}

public class SetModuleActiveRequest
{
    public bool IsActive { get; set; }
}

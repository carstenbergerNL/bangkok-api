using Bangkok.Application.Dto.Platform;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalPolicy")]
[Produces("application/json")]
[Authorize(Roles = "SuperAdmin")]
[SwaggerTag("Platform Admin (Super Admin only): dashboard stats, tenant list, suspend, upgrade, usage.")]
public class PlatformAdminController : ControllerBase
{
    private readonly IPlatformAdminService _platformAdminService;
    private readonly ILogger<PlatformAdminController> _logger;

    public PlatformAdminController(IPlatformAdminService platformAdminService, ILogger<PlatformAdminController> logger)
    {
        _platformAdminService = platformAdminService;
        _logger = logger;
    }

    [HttpGet("dashboard/stats")]
    [ProducesResponseType(typeof(ApiResponse<PlatformDashboardStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Get dashboard stats", Description = "Total tenants, active subscriptions, MRR, trial users, churned users. SuperAdmin only.")]
    public async Task<ActionResult<ApiResponse<PlatformDashboardStatsResponse>>> GetDashboardStats(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var stats = await _platformAdminService.GetDashboardStatsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<PlatformDashboardStatsResponse>.Ok(stats, correlationId));
    }

    [HttpGet("tenants")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlatformTenantListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "List tenants", Description = "All tenants with plan, subscription status, and usage. SuperAdmin only.")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlatformTenantListItemResponse>>>> GetTenants(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var list = await _platformAdminService.GetTenantsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<PlatformTenantListItemResponse>>.Ok(list, correlationId));
    }

    [HttpGet("tenants/{id:guid}/usage")]
    [ProducesResponseType(typeof(ApiResponse<TenantUsageDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get tenant usage", Description = "Usage detail for a tenant. SuperAdmin only.")]
    public async Task<ActionResult<ApiResponse<TenantUsageDetailResponse>>> GetTenantUsage([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var usage = await _platformAdminService.GetTenantUsageAsync(id, cancellationToken).ConfigureAwait(false);
        if (usage == null)
            return NotFound(ApiResponse<TenantUsageDetailResponse>.Fail(new ErrorResponse { Code = "TENANT_NOT_FOUND", Message = "Tenant not found." }, correlationId));
        return Ok(ApiResponse<TenantUsageDetailResponse>.Ok(usage, correlationId));
    }

    [HttpPut("tenants/{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Suspend tenant", Description = "Sets tenant status to Suspended. SuperAdmin only.")]
    public async Task<IActionResult> SuspendTenant([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var ok = await _platformAdminService.SuspendTenantAsync(id, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "TENANT_NOT_FOUND", Message = "Tenant not found." }, correlationId));
        _logger.LogInformation("Tenant {TenantId} suspended by platform admin.", id);
        return NoContent();
    }

    [HttpPut("tenants/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Set tenant status", Description = "Set tenant status to Active, Suspended, or Cancelled. SuperAdmin only.")]
    public async Task<IActionResult> SetTenantStatus([FromRoute] Guid id, [FromBody] SetTenantStatusRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request?.Status == null || !new[] { "Active", "Suspended", "Cancelled" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Status must be Active, Suspended, or Cancelled." }, correlationId));
        var ok = await _platformAdminService.SetTenantStatusAsync(id, request.Status, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "TENANT_NOT_FOUND", Message = "Tenant not found." }, correlationId));
        _logger.LogInformation("Tenant {TenantId} status set to {Status} by platform admin.", id, request.Status);
        return NoContent();
    }

    [HttpPut("tenants/{id:guid}/upgrade")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Upgrade tenant plan", Description = "Set or create subscription for tenant to the given plan. SuperAdmin only.")]
    public async Task<IActionResult> UpgradeTenant([FromRoute] Guid id, [FromBody] UpgradeTenantRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request == null || request.PlanId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "PlanId is required." }, correlationId));
        var ok = await _platformAdminService.UpgradeTenantAsync(id, request.PlanId, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "TENANT_OR_PLAN_NOT_FOUND", Message = "Tenant or plan not found." }, correlationId));
        _logger.LogInformation("Tenant {TenantId} upgraded to plan {PlanId} by platform admin.", id, request.PlanId);
        return NoContent();
    }
}

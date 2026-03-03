using Bangkok.Application.Configuration;
using Bangkok.Application.Dto.Billing;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GlobalPolicy")]
[Produces("application/json")]
[SwaggerTag("Billing: current subscription, usage, plans, Stripe Checkout and webhook.")]
public class BillingController : ControllerBase
{
    private readonly ISubscriptionLimitService _subscriptionLimitService;
    private readonly IPlanRepository _planRepository;
    private readonly IStripeBillingService _stripeBillingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        ISubscriptionLimitService subscriptionLimitService,
        IPlanRepository planRepository,
        IStripeBillingService stripeBillingService,
        ILogger<BillingController> logger)
    {
        _subscriptionLimitService = subscriptionLimitService;
        _planRepository = planRepository;
        _stripeBillingService = stripeBillingService;
        _logger = logger;
    }

    [HttpGet("usage")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionUsageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [SwaggerOperation(Summary = "Get subscription usage", Description = "Current plan, status, and usage (projects used/limit, members used/limit). Requires tenant context.")]
    public async Task<ActionResult<ApiResponse<SubscriptionUsageResponse>>> GetUsage(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        try
        {
            var usage = await _subscriptionLimitService.GetUsageAsync(cancellationToken).ConfigureAwait(false);
            if (usage == null)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<SubscriptionUsageResponse>.Fail(new ErrorResponse { Code = "TENANT_REQUIRED", Message = "Tenant context is required." }, correlationId));
            return Ok(ApiResponse<SubscriptionUsageResponse>.Ok(usage, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Billing usage failed. Ensure migrations 021–023 are applied (Tenant, Plan, TenantSubscription).");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<SubscriptionUsageResponse>.Fail(
                new ErrorResponse { Code = "BILLING_UNAVAILABLE", Message = "Billing is temporarily unavailable. Ensure database migrations have been run (021–023, including 023_add_subscription_plans.sql)." }, correlationId));
        }
    }

    [HttpGet("plans")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlanResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [SwaggerOperation(Summary = "List plans", Description = "All subscription plans (for upgrade UI).")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanResponse>>>> GetPlans(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        try
        {
            var plans = await _planRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var list = plans.Select(p => new PlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                MaxProjects = p.MaxProjects,
                MaxUsers = p.MaxUsers,
                AutomationEnabled = p.AutomationEnabled,
                StripePriceIdMonthly = p.StripePriceIdMonthly,
                StripePriceIdYearly = p.StripePriceIdYearly,
                StorageLimitMB = p.StorageLimitMB
            }).ToList();
            return Ok(ApiResponse<IReadOnlyList<PlanResponse>>.Ok(list, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Billing plans failed. Ensure migration 023_add_subscription_plans.sql is applied.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<IReadOnlyList<PlanResponse>>.Fail(
                new ErrorResponse { Code = "BILLING_UNAVAILABLE", Message = "Billing is temporarily unavailable. Ensure database migrations have been run (023_add_subscription_plans.sql)." }, correlationId));
        }
    }

    [HttpPost("create-checkout-session")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateCheckoutSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Create Stripe Checkout Session", Description = "Returns a URL to redirect the user to Stripe Checkout for the selected plan. Requires tenant context.")]
    public async Task<ActionResult<ApiResponse<CreateCheckoutSessionResponse>>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var tenantId = HttpContext.User.FindFirst("tenantId")?.Value ?? HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<CreateCheckoutSessionResponse>.Fail(new ErrorResponse { Code = "TENANT_REQUIRED", Message = "Tenant context is required." }, correlationId));

        if (request == null || string.IsNullOrEmpty(request.SuccessUrl) || string.IsNullOrEmpty(request.CancelUrl))
            return BadRequest(ApiResponse<CreateCheckoutSessionResponse>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "PlanId, SuccessUrl, and CancelUrl are required." }, correlationId));

        var result = await _stripeBillingService.CreateCheckoutSessionAsync(tenantGuid, request, cancellationToken).ConfigureAwait(false);
        if (result == null)
            return BadRequest(ApiResponse<CreateCheckoutSessionResponse>.Fail(new ErrorResponse { Code = "CHECKOUT_FAILED", Message = "Could not create checkout session. Check plan has Stripe Price IDs configured." }, correlationId));

        return Ok(ApiResponse<CreateCheckoutSessionResponse>.Ok(result, correlationId));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Stripe webhook", Description = "Receives Stripe events (subscription created/updated/deleted, payment failed). Verify Stripe-Signature header.")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;
        Request.EnableBuffering();
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            var jsonBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            Request.Body.Position = 0;

            try
            {
                await _stripeBillingService.ProcessWebhookAsync(jsonBody, signature, cancellationToken).ConfigureAwait(false);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook processing failed.");
                return BadRequest();
            }
        }

        return Ok();
    }
}

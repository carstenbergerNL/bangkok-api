using Bangkok.Application.Configuration;
using Bangkok.Application.Dto.Billing;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Bangkok.Api.Services;

public class StripeBillingService : IStripeBillingService
{
    private readonly StripeSettings _stripeSettings;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        IOptions<StripeSettings> stripeSettings,
        ITenantRepository tenantRepository,
        ITenantSubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        ILogger<StripeBillingService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _tenantRepository = tenantRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task<CreateCheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid tenantId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_stripeSettings.SecretKey))
        {
            _logger.LogWarning("Stripe SecretKey is not configured.");
            return null;
        }

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
            return null;

        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan == null)
            return null;

        var isYearly = string.Equals(request.BillingInterval, "yearly", StringComparison.OrdinalIgnoreCase);
        var priceId = isYearly ? plan.StripePriceIdYearly : plan.StripePriceIdMonthly;
        if (string.IsNullOrEmpty(priceId))
        {
            _logger.LogWarning("Plan {PlanId} has no Stripe Price ID for {Interval}. Configure StripePriceIdMonthly/StripePriceIdYearly.", plan.Id, request.BillingInterval);
            return null;
        }

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = tenantId.ToString(),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string> { ["tenant_id"] = tenantId.ToString() }
            }
        };

        if (!string.IsNullOrEmpty(tenant.StripeCustomerId))
            options.Customer = tenant.StripeCustomerId;
        else
            options.CustomerEmail = $"{tenant.Slug}@tenant.billing"; // optional; Stripe will create customer

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new CreateCheckoutSessionResponse
        {
            SessionId = session.Id,
            Url = session.Url ?? string.Empty
        };
    }

    public async Task ProcessWebhookAsync(string jsonBody, string stripeSignature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_stripeSettings.WebhookSecret))
        {
            _logger.LogWarning("Stripe WebhookSecret is not configured.");
            return;
        }

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(jsonBody, stripeSignature, _stripeSettings.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            throw;
        }

        _logger.LogInformation("Stripe webhook event: {Type} {Id}", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CustomerSubscriptionCreated:
                await HandleSubscriptionCreatedAsync(stripeEvent.Data.Object as Subscription, cancellationToken).ConfigureAwait(false);
                break;
            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Subscription, cancellationToken).ConfigureAwait(false);
                break;
            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Subscription, cancellationToken).ConfigureAwait(false);
                break;
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(stripeEvent.Data.Object as Invoice, cancellationToken).ConfigureAwait(false);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Subscription? subscription, CancellationToken cancellationToken)
    {
        if (subscription == null) return;

        var tenantIdStr = subscription.Metadata?.GetValueOrDefault("tenant_id");
        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            _logger.LogWarning("Subscription {SubId} has no tenant_id in metadata or client_reference_id.", subscription.Id);
            return;
        }

        var plan = await ResolvePlanFromSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
        if (plan == null)
        {
            _logger.LogWarning("Could not resolve plan for subscription {SubId}. Ensure Plan.StripePriceIdMonthly/Yearly are set.", subscription.Id);
            return;
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant != null && string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            await _tenantRepository.UpdateStripeCustomerIdAsync(tenantId, subscription.CustomerId ?? string.Empty, cancellationToken).ConfigureAwait(false);
        }

        var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            existing.Status = MapStripeStatus(subscription.Status);
            existing.PlanId = plan.Id;
            existing.StartDate = subscription.StartDate;
            existing.EndDate = subscription.CancelAt;
            await _subscriptionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return;
        }

        var sub = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = plan.Id,
            Status = MapStripeStatus(subscription.Status),
            StartDate = subscription.StartDate,
            EndDate = subscription.CancelAt,
            StripeSubscriptionId = subscription.Id
        };
        await _subscriptionRepository.CreateAsync(sub, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created TenantSubscription {Id} for tenant {TenantId}, plan {PlanId}.", sub.Id, tenantId, plan.Id);
    }

    private async Task HandleSubscriptionUpdatedAsync(Subscription? subscription, CancellationToken cancellationToken)
    {
        if (subscription == null) return;

        var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id, cancellationToken).ConfigureAwait(false);
        if (existing == null)
        {
            var tenantIdStr = subscription.Metadata?.GetValueOrDefault("tenant_id");
            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tenantId))
                await HandleSubscriptionCreatedAsync(subscription, cancellationToken).ConfigureAwait(false);
            return;
        }

        var plan = await ResolvePlanFromSubscriptionAsync(subscription, cancellationToken).ConfigureAwait(false);
        if (plan != null)
            existing.PlanId = plan.Id;

        existing.Status = MapStripeStatus(subscription.Status);
        existing.StartDate = subscription.StartDate;
        existing.EndDate = subscription.CancelAt;
        await _subscriptionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Updated TenantSubscription {Id} for Stripe subscription {StripeId}.", existing.Id, subscription.Id);
    }

    private async Task HandleSubscriptionDeletedAsync(Subscription? subscription, CancellationToken cancellationToken)
    {
        if (subscription == null) return;

        var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id, cancellationToken).ConfigureAwait(false);
        if (existing == null) return;

        existing.Status = "Cancelled";
        existing.EndDate = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cancelled TenantSubscription {Id} for Stripe subscription {StripeId}.", existing.Id, subscription.Id);
    }

    private async Task HandleInvoicePaymentFailedAsync(Invoice? invoice, CancellationToken cancellationToken)
    {
        if (invoice == null) return;

        // Stripe.net v48: Invoice no longer has SubscriptionId; try subscription from first line item.
        var subscriptionId = (invoice.Lines?.Data?.FirstOrDefault() as InvoiceLineItem)?.SubscriptionId;
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
                _logger.LogWarning("Payment failed for tenant {TenantId}, subscription {SubId}. Invoice {InvoiceId}.", existing.TenantId, existing.StripeSubscriptionId, invoice.Id);
        }
        else
            _logger.LogWarning("Payment failed for invoice {InvoiceId}, customer {CustomerId}.", invoice.Id, invoice.CustomerId ?? string.Empty);
    }

    private async Task<Bangkok.Domain.Plan?> ResolvePlanFromSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        if (string.IsNullOrEmpty(priceId)) return null;
        return await _planRepository.GetByStripePriceIdAsync(priceId, cancellationToken).ConfigureAwait(false);
    }

    private static string MapStripeStatus(string? status)
    {
        return status switch
        {
            "active" => "Active",
            "trialing" => "Trial",
            "past_due" => "Active",
            "canceled" or "unpaid" => "Cancelled",
            _ => status ?? "Active"
        };
    }
}

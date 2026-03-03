using Bangkok.Application.Dto.Billing;

namespace Bangkok.Application.Interfaces;

public interface IStripeBillingService
{
    /// <summary>Creates a Stripe Checkout Session for the current tenant and plan. Returns session URL for redirect.</summary>
    Task<CreateCheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid tenantId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Processes a Stripe webhook event (signature verified). Handles subscription created/updated/deleted and payment_failed.</summary>
    Task ProcessWebhookAsync(string jsonBody, string stripeSignature, CancellationToken cancellationToken = default);
}

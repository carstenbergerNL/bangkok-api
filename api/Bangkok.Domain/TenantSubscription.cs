namespace Bangkok.Domain;

public class TenantSubscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    /// <summary>Stripe Subscription ID (sub_xxx). Set from webhook.</summary>
    public string? StripeSubscriptionId { get; set; }
}

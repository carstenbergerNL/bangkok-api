namespace Bangkok.Domain;

/// <summary>
/// Tenant entity (one per company/organization for multi-tenancy).
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    /// <summary>Stripe Customer ID (cus_xxx). Set when first Checkout or customer created.</summary>
    public string? StripeCustomerId { get; set; }
    /// <summary>Tenant status: Active, Suspended, Cancelled.</summary>
    public string Status { get; set; } = "Active";
}

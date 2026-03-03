namespace Bangkok.Domain;

public class Plan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public int? MaxProjects { get; set; }
    public int? MaxUsers { get; set; }
    public bool AutomationEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Stripe Price ID for monthly billing (price_xxx). Used for Checkout.</summary>
    public string? StripePriceIdMonthly { get; set; }
    /// <summary>Stripe Price ID for yearly billing (price_xxx). Used for Checkout.</summary>
    public string? StripePriceIdYearly { get; set; }
    /// <summary>Storage limit in MB. Null = unlimited.</summary>
    public decimal? StorageLimitMB { get; set; }
}

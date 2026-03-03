namespace Bangkok.Application.Dto.Billing;

public class CreateCheckoutSessionRequest
{
    public Guid PlanId { get; set; }
    /// <summary>monthly or yearly</summary>
    public string BillingInterval { get; set; } = "monthly";
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

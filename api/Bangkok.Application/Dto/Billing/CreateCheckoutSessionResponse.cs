namespace Bangkok.Application.Dto.Billing;

public class CreateCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

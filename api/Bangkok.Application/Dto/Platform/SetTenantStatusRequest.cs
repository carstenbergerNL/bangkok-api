namespace Bangkok.Application.Dto.Platform;

public class SetTenantStatusRequest
{
    public string Status { get; set; } = string.Empty; // Active | Suspended | Cancelled
}

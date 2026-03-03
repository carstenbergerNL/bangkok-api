namespace Bangkok.Application.Interfaces;

/// <summary>
/// Current request tenant (from JWT). Set by middleware. Platform admin can bypass.
/// </summary>
public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    bool IsPlatformAdmin { get; }
}

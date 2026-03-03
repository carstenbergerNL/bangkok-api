using System.Security.Claims;
using Bangkok.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Bangkok.Api.Services;

/// <summary>
/// Reads current tenant and platform admin from JWT claims (set by middleware after auth).
/// </summary>
public class TenantContext : ITenantContext
{
    public const string TenantIdClaimType = "tenantId";
    private const string AdminPermission = "ViewAdminSettings";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(TenantIdClaimType)?.Value;
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                return null;
            return id;
        }
    }

    public bool IsPlatformAdmin =>
        _httpContextAccessor.HttpContext?.User?.HasClaim(ClaimTypes.Role, AdminPermission) == true
        || _httpContextAccessor.HttpContext?.User?.Claims?.Any(c => c.Type == ClaimTypes.Role && c.Value == AdminPermission) == true;
}

using System.Text.Json;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Http;

namespace Bangkok.Api.Middleware;

/// <summary>
/// Ensures authenticated requests (except auth and health) have a tenant context.
/// Platform admins can bypass. Returns 403 when tenant is required but missing.
/// </summary>
public class TenantEnforcementMiddleware
{
    private static readonly PathString AuthPath = new("/api/auth");
    private static readonly PathString HealthPath = new("/health");

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantEnforcementMiddleware> _logger;

    public TenantEnforcementMiddleware(RequestDelegate next, ILogger<TenantEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (context.Request.Path.StartsWithSegments(AuthPath, StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (tenantContext.IsPlatformAdmin)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (tenantContext.CurrentTenantId.HasValue)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        _logger.LogWarning("Authenticated request to {Path} rejected: no tenant in token. User may need to select a tenant.", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var body = ApiResponse<object>.Fail(new ErrorResponse
        {
            Code = "TENANT_REQUIRED",
            Message = "A tenant must be selected. Please sign in again and choose a tenant."
        }, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }
}

using System.Security.Claims;
using System.Text.Json;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Http;

namespace Bangkok.Api.Middleware;

/// <summary>
/// Verifies that the requested API path is allowed by the tenant's active modules and user-level access.
/// Returns 403 if the module is not active for the tenant or the user does not have access (TenantModuleUser).
/// Platform admins bypass the check.
/// </summary>
public class ModuleActivationMiddleware
{
    private static readonly PathString AuthPath = new("/api/auth");
    private static readonly PathString HealthPath = new("/health");

    /// <summary>
    /// Path prefix -> module key. First matching prefix wins.
    /// </summary>
    private static readonly (string Prefix, string ModuleKey)[] PathModuleMap =
    {
        ("/api/projects", "ProjectManagement"),
        ("/api/tasks", "ProjectManagement"),
        ("/api/project-templates", "ProjectManagement"),
        ("/api/comments", "ProjectManagement"),
        ("/api/attachments", "ProjectManagement"),
        ("/api/timelogs", "ProjectManagement"),
        ("/api/crm", "CRM"),
        ("/api/analytics", "Analytics"),
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ModuleActivationMiddleware> _logger;

    public ModuleActivationMiddleware(RequestDelegate next, ILogger<ModuleActivationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantModuleService tenantModuleService, ITenantContext tenantContext)
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

        var path = context.Request.Path.Value ?? "";
        var moduleKey = GetRequiredModuleKey(path);
        if (string.IsNullOrEmpty(moduleKey))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var isActive = await tenantModuleService.IsModuleActiveAsync(moduleKey, context.RequestAborted).ConfigureAwait(false);
        if (!isActive)
        {
            _logger.LogWarning("Request to {Path} rejected: module {ModuleKey} is not active for tenant.", path, moduleKey);
            await Write403Async(context, "MODULE_NOT_ACTIVE", "This module is not enabled for your organization. Contact your administrator to enable it.").ConfigureAwait(false);
            return;
        }

        if (tenantContext.IsPlatformAdmin)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await Write403Async(context, "FORBIDDEN", "User context required.").ConfigureAwait(false);
            return;
        }
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            await Write403Async(context, "FORBIDDEN", "Tenant context required.").ConfigureAwait(false);
            return;
        }

        var hasAccess = await tenantModuleService.HasModuleAccessAsync(userId, tenantId.Value, moduleKey, context.RequestAborted).ConfigureAwait(false);
        if (!hasAccess)
        {
            _logger.LogWarning("Request to {Path} rejected: user {UserId} does not have access to module {ModuleKey}.", path, userId, moduleKey);
            await Write403Async(context, "MODULE_ACCESS_DENIED", "You do not have access to this module. Contact your administrator.").ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static async Task Write403Async(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var body = ApiResponse<object>.Fail(new ErrorResponse { Code = code, Message = message }, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }

    private static string? GetRequiredModuleKey(string path)
    {
        var pathLower = path.ToLowerInvariant();
        foreach (var (prefix, moduleKey) in PathModuleMap)
        {
            if (pathLower.StartsWith(prefix.ToLowerInvariant(), StringComparison.Ordinal))
                return moduleKey;
        }
        return null;
    }
}

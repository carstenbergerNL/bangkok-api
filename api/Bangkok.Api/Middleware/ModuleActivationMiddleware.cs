using System.Text.Json;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Http;

namespace Bangkok.Api.Middleware;

/// <summary>
/// Verifies that the requested API path is allowed by the tenant's active modules.
/// Returns 403 if the endpoint belongs to a module that is not active for the current tenant.
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

    public async Task InvokeAsync(HttpContext context, ITenantModuleService tenantModuleService)
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
        if (isActive)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        _logger.LogWarning("Request to {Path} rejected: module {ModuleKey} is not active for tenant.", path, moduleKey);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var body = ApiResponse<object>.Fail(new ErrorResponse
        {
            Code = "MODULE_NOT_ACTIVE",
            Message = "This module is not enabled for your organization. Contact your administrator to enable it."
        }, context.TraceIdentifier);
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

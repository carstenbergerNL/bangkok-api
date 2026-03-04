using System.Net;
using System.Text.Json;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Hosting;

namespace Bangkok.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var correlationId = context.TraceIdentifier;
        var message = "An unexpected error occurred.";
        if (context.RequestServices.GetService<IWebHostEnvironment>() is IWebHostEnvironment env && env.EnvironmentName == "Development")
        {
            var detail = exception.InnerException?.Message ?? exception.Message;
            if (!string.IsNullOrEmpty(detail))
                message = detail;
        }

        var errorResponse = ApiResponse<object>.Fail(new ErrorResponse
        {
            Code = "INTERNAL_ERROR",
            Message = message
        }, correlationId);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options)).ConfigureAwait(false);
    }
}

using Serilog.Context;

namespace Bangkok.Api.Middleware;

public class RequestIdEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdEnricherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}

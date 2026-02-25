namespace Bangkok.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.TraceIdentifier = correlationId;
        context.Items["CorrelationId"] = correlationId;
        await _next(context).ConfigureAwait(false);
    }
}

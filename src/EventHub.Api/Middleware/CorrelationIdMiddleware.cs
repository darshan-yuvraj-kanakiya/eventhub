namespace EventHub.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                 ?? Guid.CreateVersion7().ToString();
        context.Response.Headers["X-Correlation-Id"] = id;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(context);   // Call next. If you do not, the pipeline stops here.
        }
    }
}


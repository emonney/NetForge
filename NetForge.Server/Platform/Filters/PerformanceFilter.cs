using System.Diagnostics;

namespace NetForge.Server.Platform.Filters;

/// <summary>
/// Times the handler, adds an <c>X-Response-Time</c> header, and logs a warning when a
/// handler exceeds the threshold. Apply per group.
/// </summary>
public sealed class PerformanceFilter(ILogger<PerformanceFilter> logger) : IEndpointFilter
{
    private const int WarnThresholdMs = 500;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var start = Stopwatch.GetTimestamp();
        var result = await next(context);
        var elapsed = Stopwatch.GetElapsedTime(start);

        var http = context.HttpContext;
        if (!http.Response.HasStarted)
            http.Response.Headers["X-Response-Time"] = $"{elapsed.TotalMilliseconds:F0}ms";

        if (elapsed.TotalMilliseconds > WarnThresholdMs)
            logger.LogWarning("Slow handler {Method} {Path} took {Elapsed:F0}ms",
                http.Request.Method, http.Request.Path, elapsed.TotalMilliseconds);

        return result;
    }
}

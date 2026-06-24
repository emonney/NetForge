using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace NetForge.Server.Platform.RateLimiting;

/// <summary>
/// Rate-limiting policies built on the framework limiter (no extra package). A generous global net
/// guards <c>/api/*</c> against floods; named policies layer a tighter cap onto specific groups.
/// Rejections come back as the same RFC 7807 ProblemDetails shape as every other error, with a
/// <c>Retry-After</c> header and a <c>RATE_LIMITED</c> code the SPA can branch on.
///
/// Opt a group in with <c>.RequireRateLimiting(RateLimitSetup.Api)</c> in a slice's endpoint map.
/// </summary>
public static class RateLimitSetup
{
    /// <summary>Blunt per-IP throttle for unauthenticated credential endpoints (login, register, reset, 2FA).</summary>
    public const string Auth = "auth";

    /// <summary>Opt-in per-caller window for expensive endpoints (exports, imports, report generation).</summary>
    public const string Api = "api";

    public static IServiceCollection AddRateLimitingSupport(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Safety net over /api/* only — the SPA shell and fingerprinted static assets are never
            // throttled. Partitioned per signed-in user, falling back to the client IP for anonymous calls.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                if (!http.Request.Path.StartsWithSegments("/api"))
                    return RateLimitPartition.GetNoLimiter("non-api");

                return RateLimitPartition.GetSlidingWindowLimiter(PartitionKey(http), _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                });
            });

            options.AddPolicy(Auth, http => RateLimitPartition.GetFixedWindowLimiter(IpKey(http), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

            options.AddPolicy(Api, http => RateLimitPartition.GetSlidingWindowLimiter(PartitionKey(http), _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
            }));

            options.OnRejected = WriteProblemDetailsAsync;
        });

        return services;
    }

    private static async ValueTask WriteProblemDetailsAsync(OnRejectedContext context, CancellationToken ct)
    {
        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "You've sent too many requests in a short period. Please slow down and try again shortly.",
            Type = "https://docs.netforge.dev/errors/rate-limited",
            Instance = http.Request.Path,
        };
        problem.Extensions["code"] = "RATE_LIMITED";
        problem.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;

        await http.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: ct);
    }

    // Per-user when signed in so one tenant on a shared NAT can't exhaust the bucket for everyone;
    // anonymous calls fall back to the client IP. Behind a proxy this needs ForwardedHeaders in prod.
    private static string PartitionKey(HttpContext http) =>
        http.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId ? $"user:{userId}" : IpKey(http);

    private static string IpKey(HttpContext http) =>
        $"ip:{http.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

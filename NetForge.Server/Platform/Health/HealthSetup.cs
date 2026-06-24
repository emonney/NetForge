using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace NetForge.Server.Platform.Health;

/// <summary>
/// Registers the platform health checks and the two anonymous ops probes. The rich, permission-gated
/// report that backs the /admin/health dashboard lives in the Features/Health slice (GET /api/health).
/// </summary>
public static class HealthSetup
{
    /// <summary>Checks that probe a dependency — included in the readiness probe and the dashboard.</summary>
    public const string Ready = "ready";

    public static IServiceCollection AddHealthChecksSupport(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [Ready]);

        return services;
    }

    /// <summary>
    /// Ops probes for orchestrators / load balancers: liveness ("is the process up", no dependencies
    /// touched) and readiness ("are dependencies reachable"). Both anonymous and minimal.
    /// </summary>
    public static IEndpointRouteBuilder MapPlatformHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(Ready) });
        return app;
    }
}

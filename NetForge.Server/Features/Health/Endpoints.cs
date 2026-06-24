using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;

namespace NetForge.Server.Features.Health;

/// <summary>
/// Rich, permission-gated health report behind the /admin/health dashboard: runs every registered
/// check and projects the HealthReport (overall status + per-check status/description/duration/data/
/// error). The anonymous ops probes (/health/live, /health/ready) live in Platform/Health.
/// </summary>
public sealed class HealthEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health")
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", Get).RequirePermission(HealthPermissions.Read);
    }

    private static async Task<IResult> Get(HealthCheckService health, CancellationToken ct)
    {
        var report = await health.CheckHealthAsync(ct);

        var checks = report.Entries
            .Select(e => new HealthEntryDto(
                Name: e.Key,
                Status: e.Value.Status.ToString(),
                Description: e.Value.Description,
                DurationMs: e.Value.Duration.TotalMilliseconds,
                Tags: e.Value.Tags.ToArray(),
                Error: e.Value.Exception?.Message,
                Data: e.Value.Data.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty)))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        var dto = new HealthReportDto(
            Status: report.Status.ToString(),
            TotalDurationMs: report.TotalDuration.TotalMilliseconds,
            CheckedAt: DateTimeOffset.UtcNow,
            Checks: checks);

        return Results.Ok(dto);
    }
}

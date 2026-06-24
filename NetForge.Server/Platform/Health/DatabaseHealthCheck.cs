using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetForge.Server.Data;

namespace NetForge.Server.Platform.Health;

/// <summary>Reachability of the application database plus whether any EF migration is still pending.</summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!await db.Database.CanConnectAsync(ct))
            return HealthCheckResult.Unhealthy("Cannot connect to the application database.");

        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        var data = new Dictionary<string, object>
        {
            ["provider"] = db.Database.ProviderName ?? "unknown",
            ["pendingMigrations"] = pending.Count,
        };

        return pending.Count == 0
            ? HealthCheckResult.Healthy("Database reachable; schema is up to date.", data)
            : HealthCheckResult.Degraded($"Database reachable, but {pending.Count} migration(s) pending.", data: data);
    }
}

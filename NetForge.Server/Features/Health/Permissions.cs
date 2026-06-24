using System.ComponentModel;

namespace NetForge.Server.Features.Health;

// Reflected into the permission catalog (any *Permissions class). Viewing the health dashboard exposes
// infrastructure detail (pending migrations, job-server state, storage path), so it's an admin gate.
public static class HealthPermissions
{
    [Description("View the system health dashboard")]
    public const string Read = "health.read";
}

using Microsoft.EntityFrameworkCore;

namespace NetForge.Server.Data.Seed;

/// <summary>
/// Creates/migrates the schema and seeds the initial data. Shared by app boot (Program.cs) and the demo
/// factory-reset (<see cref="NetForge.Server.Features.Sales.DemoMaintenance"/>) so the two can't drift —
/// a factory reset must reproduce exactly what a fresh boot seeds. The conditional comment guards keep the
/// per-tier seeders (MultiTenant / Demo) out of trimmed scaffolds, exactly as they were in Program.cs.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, IHostEnvironment env)
    {
        var db = services.GetRequiredService<AppDbContext>();
        // SQLite ships migrations (full history); the server providers create the schema from the model.
        if (db.Database.IsSqlite()) db.Database.Migrate();
        else db.Database.EnsureCreated();

        // Seeding is a machine write, not a user action: without this the trail opens with hundreds of
        // identical rows stamped in the same second, which buries the real history and turns every
        // activity-over-time view into one spike. DemoActivitySeeder writes the trail we actually want.
        using var _ = NetForge.Server.Platform.Auditing.AuditSuppression.Begin();

        await IdentitySeeder.SeedAsync(services);

        // Demo content (the user directory, the Sales sample data, and the activity feeds) is opt-in:
        // always in Development, or anywhere "Seed:DemoData" is true (e.g. the public demo site).
        if (env.IsDevelopment() || config.GetValue<bool>("Seed:DemoData"))
        {
            await DemoUsersSeeder.SeedAsync(services);
            await DemoActivitySeeder.SeedAsync(services);
        }
    }
}

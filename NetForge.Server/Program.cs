using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetForge.Server.Data;
using NetForge.Server.Data.Seed;
using NetForge.Server.Platform;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Health;
using NetForge.Server.Platform.MultiTenancy;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPlatform(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    // The active provider is config-driven (Database:Provider) so one build runs on any of the three, and a
    // dev/prod split is just a different value per environment. SQLite is the default. The model itself is
    // provider-agnostic — the SQLite-only value converters in AppDbContext are gated on Database.IsSqlite().
    switch ((builder.Configuration["Database:Provider"] ?? "sqlite").Trim().ToLowerInvariant())
    {
        default:
            options.UseSqlite(connectionString);
            break;
    }
    options.ConfigureWarnings(w => w
        // The model scan deliberately excludes _Template, so until the first real slice
        // lands it legitimately finds zero configs — silence that benign startup warning.
        .Ignore(CoreEventId.NoEntityTypeConfigurationsWarning)
        // A trimmed edition (Basic, or any --opt<Feature> off) drops some feature entities from the
        // model, but their tables stay in the shipped migrations (created dormant — see RELEASING.md).
        // That makes the model legitimately differ from the migrations snapshot, so don't let EF Core
        // throw PendingModelChangesWarning when Program.cs calls Database.Migrate() on boot. (In a full
        // build the model matches the snapshot, so this never fires — it's a no-op there.)
        .Ignore(RelationalEventId.PendingModelChangesWarning));
    // Tenant interceptor stamps the active tenant onto new ITenantScoped rows; resolved per scope. Core
    // (single-tenant mode stamps "default").
    options.AddInterceptors(serviceProvider.GetRequiredService<TenantInterceptor>());
});

// SQLite creates the file but not its parent directory.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

var app = builder.Build();

// Initialize the database on boot in every environment: apply pending migrations and seed the superadmin
// role + a sign-in-ready admin (idempotent; credentials from "Seed:Admin:*", required in production). Runs
// best-effort — a database hiccup is logged and degrades the app rather than crashing it. Demo content (the
// extra demo users + the Sales sample data) is opt-in: always in Development, or anywhere "Seed:DemoData" is
// true (e.g. the public demo site).
try
{
    using var scope = app.Services.CreateScope();
    // Migrate + seed via the shared seeder (Data/Seed/DatabaseSeeder) so app boot and the demo factory-reset
    // stay in lockstep. Add migrations for a server provider with `dotnet ef migrations add` when you want
    // versioned schema there — see the database notes in USER_GUIDE.
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Environment);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization (migrate + seed) failed; the app may be degraded.");
}

// Translate exceptions to ProblemDetails before anything else can swallow them.
app.UseExceptionHandler();

app.UseSerilogRequestLogging();


app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar
}

app.UseHttpsRedirection();

app.UseAuthentication();
// Shed floods using the now-known identity (per-user partitioning), before tenant resolution + handlers.
app.UseRateLimiter();
app.UseAuthorization();

app.MapAllFeatures();

// Anonymous ops probes for orchestrators / load balancers (the rich dashboard report is a feature slice).
app.MapPlatformHealthChecks();


app.MapFallbackToFile("/index.html");

app.Run();

// Top-level statements compile into an internal Program class; this makes it public so the
// integration test project can target WebApplicationFactory<Program>. No other purpose.
public partial class Program;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetForge.Server.Data;

namespace NetForge.Tests.Integration.Fixtures;

/// <summary>
/// Boots the real application pipeline in-memory (every slice, filter, the global ProblemDetails
/// handler, authorization) against a throwaway SQLite database, so a test exercises an endpoint exactly
/// as production would. Two deliberate swaps: the database points at a temp file (a real file, not
/// shared-cache in-memory, so the background audit writer's own connection can write concurrently), and
/// authentication runs through <see cref="TestAuthHandler"/> instead of cookies.
///
/// The environment is "Testing" (not Development), so Program.cs skips its boot-time migrate + dev
/// seeders; this fixture instead creates the schema from the model and seeds the shared Bogus catalog
/// once (see <see cref="InitializeAsync"/>). Shared across the integration suite via a collection fixture.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string LoginEmail = "integration-admin@netforge.test";
    public const string LoginPassword = "IntegrationP@ss1";

    // A real file (deleted on dispose) rather than ":memory:" — multiple connections (the request scope
    // plus the background AuditWriter) can open it without the single-connection in-memory limitation.
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"netforge-test-{Guid.NewGuid():N}.db");
    private readonly string _hangfirePath = Path.Combine(Path.GetTempPath(), $"netforge-test-hangfire-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Keep Hangfire's own SQLite store out of the repo working tree.
        builder.UseSetting("ConnectionStrings:Hangfire", _hangfirePath);

        builder.ConfigureTestServices(services =>
        {
            // Repoint the app DbContext at the throwaway SQLite file, preserving the audit interceptor.
            var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor is not null) services.Remove(optionsDescriptor);

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite($"Data Source={_dbPath}");
            });

            // Make the header-driven test scheme the default authenticator/challenger. AddIdentity pins
            // DefaultAuthenticate/Challenge to the cookie scheme explicitly, so overriding DefaultScheme
            // alone isn't enough — all three must point at "Test". The cookie scheme stays registered
            // (SignInManager signs into it directly), so the real login endpoint keeps working.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>An <see cref="HttpClient"/> whose requests authenticate as <paramref name="userId"/> with
    /// the given permission claims (none ⇒ authenticated but unauthorized).</summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", params string[] permissions)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        if (permissions.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, string.Join(',', permissions));
        return client;
    }


    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(); // schema from the model (incl. the HasData "default" tenant)


        // A confirmed user for the real login-flow test.
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(LoginEmail) is null)
        {
            var user = new AppUser { UserName = LoginEmail, Email = LoginEmail, EmailConfirmed = true };
            var result = await users.CreateAsync(user, LoginPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException("Seed user creation failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        TryDelete(_dbPath);
        TryDelete(_hangfirePath);

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { /* a background writer may still hold it; the OS reaps temp files */ }
        }
    }
}

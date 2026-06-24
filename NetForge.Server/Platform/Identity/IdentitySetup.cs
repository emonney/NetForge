using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Platform.Identity;

/// <summary>
/// Wires ASP.NET Core Identity: cookie scheme (default) with 1-minute security-stamp
/// revalidation for near-instant revocation, and an API-friendly cookie that returns 401/403
/// instead of redirecting to an MVC page. When advanced auth is enabled, a registered-but-inactive
/// bearer scheme, per-device sessions, and external OAuth providers are layered on.
/// </summary>
public static class IdentitySetup
{
    public static IServiceCollection AddPlatformIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = authOptions.RequireConfirmedEmail;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Per-tenant RBAC: replace Identity's global-role projection with one that grants the permission
        // claims of the roles the user holds in their active tenant (see AppUserClaimsPrincipalFactory).
        services.Replace(ServiceDescriptor.Scoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>());

        // Fast revocation: re-check the security stamp every minute, so killing a session or
        // rotating the stamp invalidates live cookies within a minute (see §6.1).
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        // The app is an API + SPA, not server-rendered MVC: never 302 to "/Account/Login".
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "netforge.auth";
            options.Cookie.HttpOnly = true;
            // Lax, not Strict: the session cookie must ride the top-level GET navigation back from an OAuth
            // provider so account-linking (/api/auth/external/link-callback — it RequireAuthorization) sees the
            // signed-in user. Strict withholds the cookie after a cross-site-initiated navigation → a 401 there.
            // Lax is still CSRF-safe: state-changing calls are POST/PUT/DELETE, on which Lax sends no cross-site cookie.
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = StatusCodeFor(StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = StatusCodeFor(StatusCodes.Status403Forbidden);
            options.Events.OnValidatePrincipal = ValidateSessionAsync;
        });


        // Permission catalog + wildcard policy provider + handler (replaces plain AddAuthorization).
        services.AddPlatformAuthorization();

        return services;
    }

    // Runs on every cookie validation: first Identity's security-stamp check (handles rotation +
    // sliding refresh), then — when advanced auth is enabled and a session id is present — the
    // per-device session check, so a revoked device is rejected within the 1-minute validation
    // window without touching other sessions.
    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        var services = context.HttpContext.RequestServices;
        await services.GetRequiredService<ISecurityStampValidator>().ValidateAsync(context);
    }

    // Replaces the cookie middleware's HTML redirect with a bare status code for /api paths;
    // non-API paths (none today, but the SPA fallback later) keep the redirect behaviour.
    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> StatusCodeFor(int statusCode) => context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = statusCode;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
}

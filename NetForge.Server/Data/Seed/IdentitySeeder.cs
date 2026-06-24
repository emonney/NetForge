using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Data.Seed;

/// <summary>
/// Seeds the superadmin role and a pre-confirmed admin so a fresh deployment has a working sign-in with
/// full access immediately. Idempotent; runs on boot in every environment. Credentials come from the
/// "Seed:Admin" config — local defaults in Development, required explicitly in production.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var tenantRoles = services.GetRequiredService<ITenantRoleService>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var environment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        await EnsureAdminRoleAsync(roles);
        await EnsureMemberRoleAsync(roles);

        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];

        // Dev is sign-in-ready out of the box with local defaults; production must supply explicit
        // credentials so a known-default admin is never created on a public deployment.
        if (environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(email)) email = "admin@netforge.local";
            if (string.IsNullOrWhiteSpace(password)) password = "Admin123!$";
        }
        else if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin not seeded: set Seed:Admin:Email and Seed:Admin:Password to create the initial administrator.");
            return;
        }

        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Administrator",
            };

            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                logger.LogWarning("Could not seed admin user: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Seeded admin user {Email}", email);
        }

        // Per-tenant RBAC: grant the admin the superadmin role in the default tenant (not the global
        // AspNetUserRoles, which the claims factory no longer reads). Idempotent — IsMemberAsync is true
        // once any role row exists for (admin, default).
        if (!await tenantRoles.IsMemberAsync(admin.Id, TenancyOptions.DefaultTenant))
        {
            var adminRole = await roles.FindByNameAsync(SystemRoles.Admin);
            if (adminRole is not null)
                await tenantRoles.SetRoleIdsAsync(admin.Id, TenancyOptions.DefaultTenant, [adminRole.Id]);
        }
    }

    // The Admin role carries the "*" wildcard, so it grants every permission — present and future.
    private static async Task EnsureAdminRoleAsync(RoleManager<IdentityRole> roles)
    {
        var role = await roles.FindByNameAsync(SystemRoles.Admin);
        if (role is null)
        {
            role = new IdentityRole(SystemRoles.Admin);
            await roles.CreateAsync(role);
        }

        var claims = await roles.GetClaimsAsync(role);
        if (claims.Any(c => c is { Type: PermissionClaims.ClaimType, Value: PermissionClaims.All })) return;

        await roles.AddClaimAsync(role, new Claim(PermissionClaims.ClaimType, PermissionClaims.All));
    }

    // Seeds the default sign-up role (see Account.DefaultRole). Unlike the Admin role it's seeded only
    // once: if it already exists the admin owns it, so we don't re-apply the baseline and clobber their
    // edits. It is intentionally not a protected system role — admins are meant to tune its permissions.
    private static async Task EnsureMemberRoleAsync(RoleManager<IdentityRole> roles)
    {
        if (await roles.FindByNameAsync(SystemRoles.Member) is not null) return;

        var role = new IdentityRole(SystemRoles.Member);
        if (!(await roles.CreateAsync(role)).Succeeded) return;

    }
}

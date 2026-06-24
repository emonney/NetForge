using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;

namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>
/// Projects the signed-in principal with <em>tenant-scoped</em> authorization. Unlike Identity's default
/// factory (which unions the claims of a user's global roles), this adds the role + permission claims of
/// the roles the user holds in their <see cref="AppUser.TenantId">active tenant</see>, plus a
/// <see cref="TenantClaims.ClaimType">tenant</see> claim. It re-runs on every security-stamp refresh and
/// on <c>RefreshSignInAsync</c> (the tenant switcher), so switching tenants re-projects permissions from
/// the durable <c>TenantUserRole</c> rows — nothing tenant-specific is cached in the cookie beyond the id.
/// </summary>
public sealed class AppUserClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITenantRoleService tenantRoles,
    IOptions<TenancyOptions> tenancy,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser>(userManager, options)
{
    private readonly TenancyOptions _tenancy = tenancy.Value;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        // Base adds the id/name/email/security-stamp claims but no roles (single-arg factory).
        var identity = await base.GenerateClaimsAsync(user);

        // Single-tenant mode pins everything to the default tenant, ignoring any stale active-tenant
        // column, so data isolation and permissions can't drift apart when tenancy is off.
        var tenantId = _tenancy.Mode == TenancyMode.MultiTenant ? user.TenantId : TenancyOptions.DefaultTenant;
        identity.AddClaim(new Claim(TenantClaims.ClaimType, tenantId));

        var roleClaimType = Options.ClaimsIdentity.RoleClaimType;
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in await tenantRoles.RoleNamesAsync(user.Id, tenantId))
        {
            identity.AddClaim(new Claim(roleClaimType, roleName));

            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            foreach (var claim in await roleManager.GetClaimsAsync(role))
                if (claim.Type == PermissionClaims.ClaimType)
                    permissions.Add(claim.Value);
        }

        foreach (var permission in permissions)
            identity.AddClaim(new Claim(PermissionClaims.ClaimType, permission));

        return identity;
    }
}

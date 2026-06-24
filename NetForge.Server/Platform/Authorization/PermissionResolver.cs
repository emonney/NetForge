using Microsoft.AspNetCore.Identity;

namespace NetForge.Server.Platform.Authorization;

/// <summary>
/// Resolves the union of permission claims granted by a set of roles. The signed-in principal already
/// carries these claims, but login/2FA responses build the SPA identity before that principal is
/// reissued, so the DTO mapping resolves them straight from the roles instead.
/// </summary>
public sealed class PermissionResolver(RoleManager<IdentityRole> roles)
{
    public async Task<string[]> ForRolesAsync(IEnumerable<string> roleNames)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roleNames)
        {
            var role = await roles.FindByNameAsync(roleName);
            if (role is null) continue;

            foreach (var claim in await roles.GetClaimsAsync(role))
                if (claim.Type == PermissionClaims.ClaimType)
                    permissions.Add(claim.Value);
        }

        return permissions.ToArray();
    }
}

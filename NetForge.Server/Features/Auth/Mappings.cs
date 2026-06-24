using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Features.Auth;

internal static class AuthMappings
{
    public static AuthUserDto ToAuthDto(
        this AppUser user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            user.Locale,
            user.TimeZone,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.PasswordHash is not null,
            roles,
            permissions);

    // Projects the SPA identity: the user's roles in their active tenant plus the effective permission set
    // (union across those roles, wildcards included) so the UI gates on the same grants the API enforces.
    public static async Task<AuthUserDto> ToAuthDtoAsync(
        this AppUser user, ITenantRoleService tenantRoles, PermissionResolver permissions)
    {
        var roles = (await tenantRoles.RoleNamesAsync(user.Id, user.TenantId)).ToArray();
        return user.ToAuthDto(roles, await permissions.ForRolesAsync(roles));
    }
}

namespace NetForge.Server.Platform.Authorization;

/// <summary>Built-in roles seeded at startup. Those in <see cref="All"/> are protected in the
/// role-management UI — can't be renamed, deleted, or have their permissions edited — so an admin
/// can't accidentally strip the app of its only superadmin.</summary>
public static class SystemRoles
{
    /// <summary>Superadmin. Seeded with the <see cref="PermissionClaims.All"/> wildcard, so it grants
    /// every permission, including ones added by features that don't exist yet.</summary>
    public const string Admin = "Admin";

    /// <summary>Default role granted to new sign-ups (see <c>Account.DefaultRole</c>). Seeded with a
    /// baseline read-only permission set, but deliberately <em>not</em> in <see cref="All"/>: it's a
    /// normal role admins are meant to tune (or replace) from <c>/admin/roles</c>.</summary>
    public const string Member = "Member";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Admin };

    public static bool IsSystem(string roleName) => All.Contains(roleName);
}

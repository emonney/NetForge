namespace NetForge.Server.Platform.Authorization;

/// <summary>
/// Permissions are carried as principal claims of type <see cref="ClaimType"/>. A role stores its
/// permissions as role claims (same type); the default <c>UserClaimsPrincipalFactory</c> projects
/// every role claim onto the signed-in principal, so a user's effective permission set is the union
/// across their roles — refreshed within the cookie's <c>ValidationInterval</c> when roles change.
/// </summary>
public static class PermissionClaims
{
    public const string ClaimType = "permission";

    /// <summary>The superadmin wildcard. A principal granted this satisfies every permission check.</summary>
    public const string All = "*";

    /// <summary>
    /// True when a granted permission satisfies a required one. Exact match, the global <c>*</c>, or a
    /// trailing wildcard whose prefix covers the requirement (<c>users.*</c> grants <c>users.read</c>).
    /// Matching is case-insensitive to keep callers honest about the lowercase convention.
    /// </summary>
    public static bool Grants(string granted, string required)
    {
        if (granted == All) return true;
        if (string.Equals(granted, required, StringComparison.OrdinalIgnoreCase)) return true;
        if (!granted.EndsWith(".*", StringComparison.Ordinal)) return false;

        // "users.*" → prefix "users." ; requirement must start with it (covers nested "a.b.*").
        var prefix = granted[..^1];
        return required.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when any granted permission satisfies the requirement.</summary>
    public static bool Satisfies(IEnumerable<string> granted, string required) =>
        granted.Any(g => Grants(g, required));
}

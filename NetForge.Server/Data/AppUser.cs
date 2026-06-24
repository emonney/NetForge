using Microsoft.AspNetCore.Identity;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Data;

/// <summary>
/// Application user. Identity supplies the auth columns (email, password hash, security
/// stamp, 2FA, lockout); these extras carry the profile NetForge's UI edits. Sessions and
/// OAuth links are separate tables, not columns here.
/// </summary>
public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>IANA time zone id (e.g. "America/New_York"). Drives date rendering.</summary>
    public string? TimeZone { get; set; }

    /// <summary>UI language code (e.g. "en", "ar"). Defaults to the app's default locale.</summary>
    public string? Locale { get; set; }

    /// <summary>The tenant the user is currently operating in ("active" tenant). The custom claims factory
    /// projects this tenant's role permissions onto the principal; the switcher updates it and re-signs in.
    /// The set of tenants a user may switch to is the distinct tenants of their TenantUserRole rows.</summary>
    public string TenantId { get; set; } = TenancyOptions.DefaultTenant;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

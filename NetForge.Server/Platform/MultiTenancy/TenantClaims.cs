namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>The principal carries its active tenant as a claim of this type, set by the claims factory.
/// The UserClaim resolution strategy reads it; everything else resolves the tenant from the request.</summary>
public static class TenantClaims
{
    public const string ClaimType = "tenant";
}

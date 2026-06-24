namespace NetForge.Server.Platform.MultiTenancy;

public enum TenancyMode
{
    SingleTenant,
    MultiTenant,
}

/// <summary>Bound from the "Tenancy" config section. SingleTenant by default — the tenant UI stays invisible.</summary>
public sealed class TenancyOptions
{
    public const string DefaultTenant = "default";

    public TenancyMode Mode { get; set; } = TenancyMode.SingleTenant;

    /// <summary>Multi-tenant resolution strategy: Subdomain | Path | Header | UserClaim. Used in Phase 11.</summary>
    public string Resolution { get; set; } = "Subdomain";
}

/// <summary>
/// Marker for tenant-owned entities. The DbContext adds a global query filter
/// (WHERE TenantId = current) to every entity implementing this.
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; set; }
}

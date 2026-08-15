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
/// <para>
/// <b>Leave <see cref="TenantId"/> unset</b> (empty) on new rows — <see cref="TenantInterceptor"/> stamps
/// the active tenant, and only skips rows that already carry one so a seeder can write cross-tenant data.
/// A property initializer therefore reads as "already stamped": defaulting it to <c>DefaultTenant</c>
/// silently writes <em>every</em> row of that entity into the default tenant, where the query filter can
/// never see it again.
/// </para>
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; set; }
}

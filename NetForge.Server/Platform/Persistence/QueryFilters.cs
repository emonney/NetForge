namespace NetForge.Server.Platform.Persistence;

/// <summary>
/// Names for the global query filters (EF Core 10 named filters), so a query can selectively disable
/// one with <c>IgnoreQueryFilters([QueryFilters.SoftDelete])</c> while keeping the others — notably,
/// reaching soft-deleted rows without also dropping tenant isolation.
/// </summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";
    public const string Tenant = "Tenant";
}

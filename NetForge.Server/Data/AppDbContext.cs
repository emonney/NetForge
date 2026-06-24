using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.Persistence;

namespace NetForge.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
    : IdentityDbContext<AppUser>(options)
{
    private readonly ITenantContext _tenant = tenant;

    // Read live per query — NOT captured in the constructor. The DbContext can be constructed before the
    // tenant is resolved (e.g. during cookie/session validation in UseAuthentication, which queries the
    // DB), so capturing tenant.TenantId in the ctor would freeze the filter to "default" for the whole
    // request. EF parameterizes this instance-property access and re-evaluates it when each query runs —
    // after resolution — so the filter always uses the active tenant.
    private string CurrentTenantId => _tenant.TenantId;

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo SetSoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply every slice's IEntityTypeConfiguration, except underscore-prefixed
        // copy-source templates (e.g. Features/_Template). Those are scaffolding, not
        // live entities — mirrors generouted's _-prefix ignore rule on the frontend.
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => !(type.Namespace?.Split('.').Any(segment => segment.StartsWith('_')) ?? false));

        // Tenant isolation: every ITenantScoped entity gets a global WHERE TenantId = current.
        // Single-tenant mode filters by a constant, which the query planner optimizes away.
        foreach (var entityType in builder.Model.GetEntityTypes())
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                SetTenantFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [builder]);

        // Soft delete: every ISoftDeletable entity gets a global WHERE IsDeleted = 0, so deleted rows
        // vanish until a query opts in with IgnoreQueryFilters(). Both filters are *named* (EF Core 10),
        // so an entity that is both tenant-scoped and soft-deletable (e.g. Product) gets both AND-ed —
        // and a "deleted" view can drop just the soft-delete filter (IgnoreQueryFilters([SoftDelete]))
        // without losing tenant isolation.
        foreach (var entityType in builder.Model.GetEntityTypes())
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                SetSoftDeleteFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [builder]);

        // SQLite-only: store DateTimeOffset as UTC ticks and decimal as integer cents so those columns
        // sort/range-filter correctly in SQL (the provider can't compare either as stored). See
        // SqliteValueConverters. Postgres/SQL Server keep native types when migrations are regenerated.
        if (Database.IsSqlite())
            foreach (var property in builder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(SqliteValueConverters.DateTimeOffsetToTicks);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(SqliteValueConverters.NullableDateTimeOffsetToTicks);
                else if (property.ClrType == typeof(decimal))
                    property.SetValueConverter(SqliteValueConverters.DecimalToCents);
                else if (property.ClrType == typeof(decimal?))
                    property.SetValueConverter(SqliteValueConverters.NullableDecimalToCents);
            }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantScoped =>
        builder.Entity<TEntity>().HasQueryFilter(QueryFilters.Tenant, e => e.TenantId == CurrentTenantId);

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : class, ISoftDeletable =>
        builder.Entity<TEntity>().HasQueryFilter(QueryFilters.SoftDelete, e => !e.IsDeleted);
}

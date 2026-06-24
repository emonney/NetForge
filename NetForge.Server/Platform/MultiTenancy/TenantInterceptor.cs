using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>
/// Stamps the active tenant onto new <see cref="ITenantScoped"/> rows that don't already carry one, so
/// handlers don't each have to remember to set <c>TenantId</c>. An explicit value (e.g. a seeder writing
/// cross-tenant data) is left untouched. Scoped — one per request, reading the resolved tenant; in
/// single-tenant mode it stamps the <c>"default"</c> constant.
/// </summary>
public sealed class TenantInterceptor(ITenantContext tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        foreach (var entry in context.ChangeTracker.Entries())
            if (entry is { State: EntityState.Added, Entity: ITenantScoped { TenantId: var id } scoped } && string.IsNullOrEmpty(id))
                scoped.TenantId = tenant.TenantId;
    }
}

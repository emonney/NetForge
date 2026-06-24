using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;

namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>
/// Per-tenant role assignment — the application's source of truth for "which roles does this user hold
/// in this tenant", replacing Identity's global <c>AspNetUserRoles</c>. The claims factory, the login
/// DTO mapping, and the Users / Tenancy admin endpoints all go through here, so every role grant is
/// tenant-scoped. Role <em>definitions</em> (and their permission claims) remain global in <c>AspNetRoles</c>.
/// </summary>
public interface ITenantRoleService
{
    /// <summary>Distinct role names the user holds in the tenant (joined to the global role catalog).</summary>
    Task<IReadOnlyList<string>> RoleNamesAsync(string userId, string tenantId, CancellationToken ct = default);

    /// <summary>The distinct tenants the user belongs to — i.e. the tenants they may switch to.</summary>
    Task<IReadOnlyList<string>> TenantIdsForUserAsync(string userId, CancellationToken ct = default);

    Task<bool> IsMemberAsync(string userId, string tenantId, CancellationToken ct = default);

    /// <summary>Replaces the user's role set in the tenant with exactly <paramref name="roleIds"/> (diffing
    /// adds/removes). Removing every role drops the membership — the user can no longer switch to the tenant.</summary>
    Task SetRoleIdsAsync(string userId, string tenantId, IReadOnlyCollection<string> roleIds, CancellationToken ct = default);

    /// <summary>Grants a single role in a tenant without disturbing existing ones (idempotent) — the
    /// invitation-accept path, where the user may already be a member.</summary>
    Task GrantRoleAsync(string userId, string tenantId, string roleId, CancellationToken ct = default);

    Task RemoveFromTenantAsync(string userId, string tenantId, CancellationToken ct = default);

    /// <summary>Distinct users holding a role across all tenants — powers the role-management "members" count.</summary>
    Task<int> CountUsersInRoleAsync(string roleId, CancellationToken ct = default);

    /// <summary>Drops every assignment of a role (used when the role is deleted) so no grants are orphaned.</summary>
    Task RemoveRoleEverywhereAsync(string roleId, CancellationToken ct = default);
}

public sealed class TenantRoleService(AppDbContext db) : ITenantRoleService
{
    private DbSet<TenantUserRole> Assignments => db.Set<TenantUserRole>();

    public async Task<IReadOnlyList<string>> RoleNamesAsync(string userId, string tenantId, CancellationToken ct = default) =>
        await Assignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .Join(db.Roles, r => r.RoleId, role => role.Id, (_, role) => role.Name!)
            .OrderBy(name => name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> TenantIdsForUserAsync(string userId, CancellationToken ct = default) =>
        await Assignments
            .Where(r => r.UserId == userId)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(ct);

    public Task<bool> IsMemberAsync(string userId, string tenantId, CancellationToken ct = default) =>
        Assignments.AnyAsync(r => r.UserId == userId && r.TenantId == tenantId, ct);

    public async Task SetRoleIdsAsync(
        string userId, string tenantId, IReadOnlyCollection<string> roleIds, CancellationToken ct = default)
    {
        var target = roleIds.ToHashSet();
        var existing = await Assignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .ToListAsync(ct);
        var existingIds = existing.Select(r => r.RoleId).ToHashSet();

        Assignments.RemoveRange(existing.Where(r => !target.Contains(r.RoleId)));
        foreach (var roleId in target.Where(id => !existingIds.Contains(id)))
            Assignments.Add(new TenantUserRole { UserId = userId, TenantId = tenantId, RoleId = roleId });

        await db.SaveChangesAsync(ct);
    }

    public async Task GrantRoleAsync(string userId, string tenantId, string roleId, CancellationToken ct = default)
    {
        if (await Assignments.AnyAsync(r => r.UserId == userId && r.TenantId == tenantId && r.RoleId == roleId, ct))
            return;
        Assignments.Add(new TenantUserRole { UserId = userId, TenantId = tenantId, RoleId = roleId });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveFromTenantAsync(string userId, string tenantId, CancellationToken ct = default)
    {
        var rows = await Assignments
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .ToListAsync(ct);
        Assignments.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountUsersInRoleAsync(string roleId, CancellationToken ct = default) =>
        Assignments.Where(r => r.RoleId == roleId).Select(r => r.UserId).Distinct().CountAsync(ct);

    public async Task RemoveRoleEverywhereAsync(string roleId, CancellationToken ct = default)
    {
        var rows = await Assignments.Where(r => r.RoleId == roleId).ToListAsync(ct);
        Assignments.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }
}

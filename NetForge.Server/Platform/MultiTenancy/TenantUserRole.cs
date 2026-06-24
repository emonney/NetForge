using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>
/// A per-tenant role grant — the source of truth for "what may this user do in this tenant". It replaces
/// Identity's global <c>AspNetUserRoles</c> for application authorization: the custom claims factory
/// projects the permission claims of the roles a user holds <em>in their active tenant</em> onto the
/// principal. Role <em>definitions</em> (and their permission claims) stay global in <c>AspNetRoles</c> —
/// this table only records the assignment. Not <see cref="ITenantScoped"/>: it is queried by explicit
/// tenant id (e.g. at sign-in, before any request tenant is resolved), never via the global filter.
/// </summary>
public sealed class TenantUserRole
{
    public int Id { get; set; }

    public required string TenantId { get; set; }

    /// <summary>FK to <c>AspNetUsers.Id</c>.</summary>
    public required string UserId { get; set; }

    /// <summary>FK to <c>AspNetRoles.Id</c> (the role id, not its name — survives a rename).</summary>
    public required string RoleId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class TenantUserRoleConfig : IEntityTypeConfiguration<TenantUserRole>
{
    public void Configure(EntityTypeBuilder<TenantUserRole> builder)
    {
        builder.ToTable("TenantUserRoles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RoleId).HasMaxLength(450).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.UserId });
        builder.HasIndex(x => new { x.UserId });
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.RoleId }).IsUnique();
    }
}

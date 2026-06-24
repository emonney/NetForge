using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NetForge.Server.Platform.MultiTenancy;

public enum TenantStatus
{
    Active,
    Suspended,
}

/// <summary>
/// A tenant (organisation) in the system. The <see cref="Id"/> <em>is</em> the string TenantId carried
/// by every <see cref="ITenantScoped"/> row and by the resolution strategies — for subdomain resolution
/// it doubles as the subdomain label (e.g. <c>acme</c> → <c>acme.app.com</c>). The platform default
/// tenant (<see cref="TenancyOptions.DefaultTenant"/>) always exists; single-tenant mode only ever uses it.
/// </summary>
public sealed class Tenant
{
    /// <summary>Stable slug, lowercased; the TenantId and (for subdomain resolution) the subdomain label.</summary>
    public required string Id { get; set; }

    public required string Name { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>Brand colour applied as the primary CSS variable when this tenant is active (hex or oklch).</summary>
    public string? PrimaryColor { get; set; }

    public string? LogoUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PrimaryColor).HasMaxLength(32);
        builder.Property(x => x.LogoUrl).HasMaxLength(2048);

        // The platform default tenant always exists — every user/row references it, and single-tenant
        // mode only ever uses it. Seeded via the model so it's present after migration in prod too,
        // not only under the dev seeders. Fixed CreatedAt keeps the migration deterministic.
        builder.HasData(new Tenant
        {
            Id = TenancyOptions.DefaultTenant,
            Name = "Default",
            Status = TenantStatus.Active,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}

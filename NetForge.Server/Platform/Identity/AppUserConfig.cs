using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetForge.Server.Data;
using NetForge.Server.Platform.Auditing;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Platform.Identity;

/// <summary>
/// Augments Identity's own AppUser mapping: bounds the profile columns and redacts the
/// secret-bearing inherited columns from audit logs (they can't carry the [Sensitive]
/// attribute because they're declared on IdentityUser).
/// </summary>
internal sealed class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(100);
        builder.Property(x => x.AvatarUrl).HasMaxLength(2048);
        builder.Property(x => x.TimeZone).HasMaxLength(100);
        builder.Property(x => x.Locale).HasMaxLength(16);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue(TenancyOptions.DefaultTenant);

        builder.Property(x => x.PasswordHash).MarkSensitive();
        builder.Property(x => x.SecurityStamp).MarkSensitive();
        builder.Property(x => x.ConcurrencyStamp).MarkSensitive();
    }
}

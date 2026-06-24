using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetForge.Server.Platform.Auditing;

namespace NetForge.Server.Platform.Identity;

/// <summary>
/// One row per interactive sign-in. Its <see cref="Id"/> is carried in the auth cookie as the
/// "sid" claim, so a session can be shown ("this device") and revoked individually. Audit-exempt:
/// LastSeen churns every validation interval; sign-in/revoke are logged as explicit audit events.
/// </summary>
public sealed class UserSession : IAuditExempt
{
    public required string Id { get; set; }

    public required string UserId { get; set; }

    public string? DeviceName { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

internal sealed class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(64);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
    }
}

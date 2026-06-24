using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NetForge.Server.Platform.Settings;

/// <summary>A persisted setting value at one scope. ScopeId is null for App, else tenant/user id.</summary>
public sealed class Setting
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public SettingScope Scope { get; set; }
    public string? ScopeId { get; set; }
    public required string ValueJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

internal sealed class SettingConfig : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScopeId).HasMaxLength(450);
        builder.Property(x => x.UpdatedBy).HasMaxLength(450);
        builder.HasIndex(x => new { x.Key, x.Scope, x.ScopeId }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NetForge.Server.Features._Template;

// Skipped while under the _Template namespace (see AppDbContext.OnModelCreating).
// Copy + rename to activate, then: dotnet ef migrations add Add{Domain}.
internal sealed class TemplateItemConfig : IEntityTypeConfiguration<TemplateItem>
{
    public void Configure(EntityTypeBuilder<TemplateItem> builder)
    {
        builder.ToTable("TemplateItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
    }
}

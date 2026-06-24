using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetForge.Server.Platform.Auditing;

namespace NetForge.Server.Features.Comments;

/// <summary>
/// A user-authored comment attached to any entity by <c>(EntityType, EntityId)</c> — the same coordinate
/// the audit timeline uses, so a record's detail page can show both its change history and its
/// discussion. <c>@mentions</c> in the body notify the named users. Audit-exempt: comments are content,
/// not domain changes, so they don't belong in the audit trail.
/// </summary>
public sealed class Comment : IAuditExempt
{
    public int Id { get; set; }

    /// <summary>CLR type name of the commented-on entity (e.g. "Order"), matching the audit log's key.</summary>
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }

    public required string AuthorId { get; set; }
    /// <summary>Author's display name snapshotted at post time, so the thread reads even if they're renamed.</summary>
    public required string AuthorName { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class CommentConfig : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AuthorId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.AuthorName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();

        // The detail-page query is always "comments for this entity, newest first".
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}

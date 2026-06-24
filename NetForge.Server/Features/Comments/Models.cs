namespace NetForge.Server.Features.Comments;

/// <summary>Wire shape of a comment. <see cref="CanDelete"/> is resolved per caller (author or moderator)
/// so the UI can show a delete affordance without re-deriving the rule.</summary>
public record CommentDto(
    int Id,
    string EntityType,
    string EntityId,
    string AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Body,
    bool CanDelete,
    DateTimeOffset CreatedAt);

/// <summary><see cref="Url"/> is the deep link the FE is on; it rides into any @mention notification so the
/// recipient lands on the right record.</summary>
public record CreateCommentRequest(string Body, string? Url = null);

/// <summary>A user the composer can @mention. <see cref="Token"/> is what gets inserted after the "@".</summary>
public record MentionableUserDto(string Id, string Name, string Token, string? AvatarUrl);

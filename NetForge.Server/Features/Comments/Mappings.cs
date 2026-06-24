namespace NetForge.Server.Features.Comments;

public static class CommentMappings
{
    public static CommentDto ToDto(this Comment comment, bool canDelete, string? authorAvatarUrl) =>
        new(comment.Id, comment.EntityType, comment.EntityId, comment.AuthorId, comment.AuthorName,
            authorAvatarUrl, comment.Body, canDelete, comment.CreatedAt);
}

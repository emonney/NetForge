using System.ComponentModel;

namespace NetForge.Server.Features.Comments;

/// <summary>
/// Commenting itself only needs authentication (any signed-in user can read and post on records they can
/// reach). The one gated capability is moderation — deleting someone else's comment.
/// </summary>
public static class CommentPermissions
{
    [Description("Delete other users' comments")]
    public const string Moderate = "comments.moderate";
}

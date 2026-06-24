using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;

namespace NetForge.Server.Features.Comments;

/// <summary>
/// Comments on any entity, keyed by <c>(entityType, entityId)</c> — the same coordinate the audit
/// timeline uses, so a record's detail page shows discussion next to history. Reading and posting need
/// only authentication; deleting is allowed for the author or anyone with <c>comments.moderate</c>.
/// <c>@mentions</c> in a comment notify the named users (resolved by display name, username, or email
/// local-part) with a deep link back to the record.
/// </summary>
public sealed partial class CommentEndpoints : IFeatureEndpoints
{
    [GeneratedRegex(@"@([A-Za-z0-9._-]+)")]
    private static partial Regex MentionPattern();

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comments")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/mentionable", Mentionable);
        group.MapGet("/{entityType}/{entityId}", List);
        group.MapPost("/{entityType}/{entityId}", Create).AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id:int}", Delete).AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(
        string entityType, string entityId, AppDbContext db, HttpContext http, CancellationToken ct)
    {
        var me = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var canModerate = Can(http, CommentPermissions.Moderate);

        var comments = await db.Set<Comment>().AsNoTracking()
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var avatars = authorIds.Count == 0
            ? new Dictionary<string, string?>()
            : await db.Set<AppUser>().AsNoTracking()
                .Where(u => authorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.AvatarUrl, ct);

        var dtos = comments
            .Select(c => c.ToDto(canModerate || c.AuthorId == me, avatars.GetValueOrDefault(c.AuthorId)))
            .ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> Create(
        string entityType, string entityId, CreateCommentRequest req,
        AppDbContext db, UserManager<AppUser> users,
        HttpContext http, CancellationToken ct)
    {
        var author = await users.GetUserAsync(http.User) ?? throw new UnauthorizedException("Sign in to comment.");
        var authorName = author.DisplayName ?? author.Email ?? author.UserName ?? "Unknown";

        var comment = new Comment
        {
            EntityType = entityType,
            EntityId = entityId,
            AuthorId = author.Id,
            AuthorName = authorName,
            Body = req.Body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Add(comment);
        await db.SaveChangesAsync(ct);


        return Results.Created($"/api/comments/{comment.Id}", comment.ToDto(canDelete: true, author.AvatarUrl));
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, HttpContext http, CancellationToken ct)
    {
        var me = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var comment = await db.Set<Comment>().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Comment", id);

        if (comment.AuthorId != me && !Can(http, CommentPermissions.Moderate))
            throw new ForbiddenException("You can only delete your own comments.");

        db.Remove(comment);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // Lightweight user directory for the composer's @mention autocomplete (any signed-in user).
    private static async Task<IResult> Mentionable(string? q, AppDbContext db, HttpContext http, CancellationToken ct)
    {
        var me = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = db.Set<AppUser>().AsNoTracking().Where(u => u.Id != me);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                (u.DisplayName != null && u.DisplayName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.UserName != null && u.UserName.Contains(term)));
        }

        var matches = await query
            .OrderBy(u => u.DisplayName ?? u.Email)
            .Take(8)
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.AvatarUrl })
            .ToListAsync(ct);

        var dtos = matches
            .Select(u => new MentionableUserDto(u.Id, u.DisplayName ?? u.Email ?? "User", MentionToken(u.DisplayName, u.Email), u.AvatarUrl))
            .ToList();
        return Results.Ok(dtos);
    }


    // The single token the composer inserts after "@" (display name without spaces, else email local-part).
    private static string MentionToken(string? displayName, string? email) =>
        !string.IsNullOrWhiteSpace(displayName)
            ? new string(displayName.Where(ch => !char.IsWhiteSpace(ch)).ToArray())
            : email?.Split('@')[0] ?? "user";

    // Every token a user could be mentioned by (lower-cased), so resolution tolerates which one was typed.
    private static IEnumerable<string> MentionTokens(string? displayName, string? email, string? userName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            yield return new string(displayName.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(email))
            yield return email.Split('@')[0].ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(userName))
            yield return userName.ToLowerInvariant();
    }

    private static bool Can(HttpContext http, string permission) =>
        PermissionClaims.Satisfies(http.User.FindAll(PermissionClaims.ClaimType).Select(c => c.Value), permission);
}

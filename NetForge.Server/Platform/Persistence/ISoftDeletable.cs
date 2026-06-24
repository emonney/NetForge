namespace NetForge.Server.Platform.Persistence;

/// <summary>
/// Marker for entities that are soft-deleted rather than physically removed. The DbContext adds a global
/// query filter (<c>WHERE IsDeleted = 0</c>) to every entity implementing this, so deleted rows vanish
/// from normal queries; delete handlers set the flag instead of calling <c>Remove</c>, and a restore
/// clears it. Use <c>IgnoreQueryFilters()</c> to reach deleted rows (a "deleted" view, or restore).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

namespace NetForge.Server.Platform.Auditing;

/// <summary>
/// Explicit audit logging for non-DB events (login, OAuth link, file download, admin actions). EF
/// entity changes are captured automatically by the audit interceptor when the Audit feature is
/// present. Editions built without Audit (e.g. NetForge Basic) register <see cref="NoopAuditService"/>,
/// so handlers can call <c>LogAsync</c> unconditionally.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string category, string action, string? entityType = null, string? entityId = null,
        object? data = null, CancellationToken cancellationToken = default);
}

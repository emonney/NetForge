namespace NetForge.Server.Platform.Auditing;

/// <summary>
/// No-op <see cref="IAuditService"/> for editions built without the Audit feature (NetForge Basic).
/// Manual <c>LogAsync</c> calls in handlers become cheap no-ops; entity-change auditing (the
/// interceptor) and the audit read UI are stripped entirely.
/// </summary>
public sealed class NoopAuditService : IAuditService
{
    public Task LogAsync(
        string category, string action, string? entityType = null, string? entityId = null,
        object? data = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

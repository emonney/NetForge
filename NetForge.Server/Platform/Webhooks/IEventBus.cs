namespace NetForge.Server.Platform.Webhooks;

/// <summary>
/// The one call a slice makes to emit a domain event for outgoing webhooks (§6.15). A handler does
/// <c>await _eventBus.PublishAsync("user.locked", payload)</c>; the dispatcher fans the event out to
/// every active subscription in the current tenant that listens for it, persisting a delivery row and
/// queueing a signed HTTP POST per match. Slices depend only on this contract — the implementation
/// lives in the Webhooks slice and is reflection-registered.
/// </summary>
public interface IEventBus
{
    /// <summary>Emit <paramref name="eventType"/> (a catalog name like <c>user.locked</c>) with an
    /// optional JSON-serializable <paramref name="payload"/> that becomes the delivery's <c>data</c>.
    /// A no-op when nothing subscribes — cheap to call unconditionally from a handler.</summary>
    Task PublishAsync(string eventType, object? payload = null, CancellationToken ct = default);
}

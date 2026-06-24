namespace NetForge.Server.Platform.Webhooks;

/// <summary>
/// No-op <see cref="IEventBus"/> for editions built without the Webhooks feature (e.g. NetForge Basic).
/// Slices still call <c>PublishAsync</c> unconditionally — it's defined to be a no-op when nothing
/// subscribes — so with the dispatcher stripped there is simply nothing to fan out to. Registered only
/// when the Webhooks feature is off; harmless and unused otherwise.
/// </summary>
public sealed class NoopEventBus : IEventBus
{
    public Task PublishAsync(string eventType, object? payload = null, CancellationToken ct = default) =>
        Task.CompletedTask;
}

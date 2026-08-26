namespace NetForge.Server.Platform.Auditing;

/// <summary>
/// Turns the EF audit interceptor off for the current async flow.
///
/// For machine-generated writes — seeding, a bulk import — the trail is worse with the entries than
/// without: hundreds of identical rows, all stamped within the same second, by no one. They bury the
/// handful of entries that record what a person actually did, and they flatten every "activity over
/// time" view into a single spike. The write still happens; only the *record of who did it* is skipped,
/// because there is no who.
///
/// <code>
/// using (AuditSuppression.Begin())
///     await SeedAsync(services);
/// </code>
///
/// Async-local, so it covers everything awaited inside the block and nothing on another request.
/// </summary>
public static class AuditSuppression
{
    private static readonly AsyncLocal<bool> Active = new();

    public static bool IsActive => Active.Value;

    public static IDisposable Begin() => new Scope();

    private sealed class Scope : IDisposable
    {
        private readonly bool _previous = Active.Value;

        public Scope() => Active.Value = true;

        public void Dispose() => Active.Value = _previous;
    }
}

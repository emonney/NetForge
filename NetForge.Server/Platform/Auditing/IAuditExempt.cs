namespace NetForge.Server.Platform.Auditing;

/// <summary>
/// Marks an entity the EF audit interceptor must ignore entirely. For high-churn, low-value
/// infrastructure tables (e.g. UserSession's per-request LastSeen) whose security-relevant moments
/// are captured by explicit <see cref="IAuditService"/> events instead.
///
/// Lives in its own file (separate from the Audit feature's implementation) so entities can carry the
/// marker in every edition — in editions without the Audit feature it's simply inert.
/// </summary>
public interface IAuditExempt;

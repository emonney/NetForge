using System.ComponentModel;

namespace NetForge.Server.Features.Users;

// The user-administration events a tenant can subscribe to over webhooks. Any *WebhookEvents class is
// reflection-discovered into the WebhookEventCatalog (mirroring how *Permissions feed the permission
// catalog), so a slice declares its events here and they appear in the subscription editor — no central
// registry edit. The Sales domain (Phase 10) adds order.* / product.* the same way.
public static class UserWebhookEvents
{
    [Description("A user account was created by an administrator")]
    public const string Created = "user.created";

    [Description("A user account was locked by an administrator")]
    public const string Locked = "user.locked";

    [Description("A user account was unlocked")]
    public const string Unlocked = "user.unlocked";

    [Description("A user account was deleted")]
    public const string Deleted = "user.deleted";

    [Description("A user's role assignments changed")]
    public const string RolesChanged = "user.roles_changed";
}

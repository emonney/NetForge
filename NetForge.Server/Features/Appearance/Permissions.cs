using System.ComponentModel;

namespace NetForge.Server.Features.Appearance;

// Reflected into the permission catalog (any *Permissions class). Changing the brand colour is an
// instance-wide, admin-only action; reading it is anonymous (the SPA needs it before sign-in).
public static class AppearancePermissions
{
    [Description("Customize the application's brand colour")]
    public const string Manage = "appearance.manage";
}

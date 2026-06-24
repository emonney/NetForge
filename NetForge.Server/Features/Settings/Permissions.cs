using System.ComponentModel;

namespace NetForge.Server.Features.Settings;

public static class SettingPermissions
{
    [Description("View application settings")]
    public const string Read = "settings.read";

    [Description("Change application settings")]
    public const string Update = "settings.update";
}

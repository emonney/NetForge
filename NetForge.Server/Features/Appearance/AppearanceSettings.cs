using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Appearance;

/// <summary>App-scoped appearance settings — an administrator sets the brand colour once for the instance.</summary>
public static class AppearanceSettings
{
    public const string Category = "Appearance";

    /// <summary>The curated theme key (e.g. <c>ocean</c>, <c>forest</c>); blank/"default" uses the built-in
    /// theme. Swaps the full light + dark palette across the app at runtime.</summary>
    public const string Theme = "Appearance.Theme";

    /// <summary>The brand accent colour as any CSS colour (e.g. <c>#4f46e5</c> or an <c>oklch(...)</c> value);
    /// blank uses the theme's accent. Overrides <c>--primary</c> on top of the chosen theme.</summary>
    public const string BrandColor = "Appearance.BrandColor";

    /// <summary>A user-defined palette as JSON (<c>{ "light": {...}, "dark": {...} }</c> of token→colour),
    /// applied when <see cref="Theme"/> is <c>"custom"</c>. Built in the customizer by forking a base theme.</summary>
    public const string CustomTheme = "Appearance.CustomTheme";
}

/// <summary>Registers appearance settings into the catalog so they resolve through <see cref="ISettingService"/>.</summary>
public sealed class AppearanceSettingsContributor : ISettingsContributor
{
    public void Register()
    {
        // Tenant-scoped so each tenant keeps its own look; the SettingService resolves the active tenant
        // automatically (single-tenant runs resolve the "default" tenant). Not App-scoped, so it stays out
        // of the raw /admin/settings list — the dedicated /admin/appearance page is its editor.
        SettingDefinitions.Register(
            AppearanceSettings.Theme, typeof(string), [SettingScope.Tenant], string.Empty, AppearanceSettings.Category);
        SettingDefinitions.Register(
            AppearanceSettings.BrandColor, typeof(string), [SettingScope.Tenant], string.Empty, AppearanceSettings.Category);
        SettingDefinitions.Register(
            AppearanceSettings.CustomTheme, typeof(string), [SettingScope.Tenant], string.Empty, AppearanceSettings.Category);
    }
}

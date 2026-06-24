namespace NetForge.Server.Platform.Settings;

public enum SettingScope
{
    App,
    Tenant,
    User,
}

/// <summary>One selectable value for a choice setting (rendered as a dropdown).</summary>
public sealed record SettingOption(string Value, string Label);

/// <summary>Declares a setting: its key, value type, supported scopes, default, and grouping. A setting
/// becomes a dropdown by declaring either a static <see cref="Options"/> list or an
/// <see cref="OptionsProvider"/> key (a reflection-discovered <c>ISettingOptionsProvider</c> for dynamic
/// choices like roles) — the admin UI renders the dropdown generically, so nothing is hard-coded there.</summary>
public sealed record SettingDefinition(
    string Key,
    Type Type,
    SettingScope[] Scopes,
    object? DefaultValue,
    string Category,
    string? DescriptionKey = null,
    IReadOnlyList<SettingOption>? Options = null,
    string? OptionsProvider = null);

/// <summary>
/// Startup registry of known settings. Features call Register(...) once at boot; the admin UI
/// (Phase 4) renders from <see cref="All"/>, and SettingService resolves defaults from it.
/// </summary>
public static class SettingDefinitions
{
    private static readonly Dictionary<string, SettingDefinition> Registry = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<SettingDefinition> All => Registry.Values;

    public static void Register(
        string key, Type type, SettingScope[] scopes, object? defaultValue,
        string category, string? descriptionKey = null,
        IReadOnlyList<SettingOption>? options = null, string? optionsProvider = null) =>
        Registry[key] = new SettingDefinition(
            key, type, scopes, defaultValue, category, descriptionKey, options, optionsProvider);

    public static SettingDefinition? Find(string key) => Registry.GetValueOrDefault(key);
}

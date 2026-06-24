namespace NetForge.Server.Platform.Settings;

/// <summary>
/// Supplies the dropdown choices for a choice setting whose options are dynamic (e.g. roles from the
/// database). A setting opts in by naming the provider's <see cref="Key"/> in its registration
/// (<c>optionsProvider: "roles"</c>). Implementations are reflection-discovered and registered in DI, so a
/// slice adds dynamic choices without editing the settings UI or Program.cs.
/// </summary>
public interface ISettingOptionsProvider
{
    /// <summary>Stable key referenced by <c>SettingDefinition.OptionsProvider</c>.</summary>
    string Key { get; }

    Task<IReadOnlyList<SettingOption>> GetOptionsAsync(CancellationToken ct);
}

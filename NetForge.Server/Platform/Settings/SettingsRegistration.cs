using System.Reflection;

namespace NetForge.Server.Platform.Settings;

/// <summary>
/// Implemented by a feature to declare its settings. Discovered by reflection at startup (like
/// <c>IFeatureEndpoints</c>) and run once — so a slice registers its settings without editing
/// Program.cs, and they appear in the admin/profile UI automatically.
/// </summary>
public interface ISettingsContributor
{
    void Register();
}

public static class SettingsRegistration
{
    /// <summary>Instantiates every <see cref="ISettingsContributor"/> and lets it register its
    /// settings. Underscore-prefixed namespaces (copy-source templates) are skipped.</summary>
    public static void RegisterAll()
    {
        var contributors = typeof(ISettingsContributor).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ISettingsContributor).IsAssignableFrom(t))
            .Where(t => !(t.Namespace?.Split('.').Any(s => s.StartsWith('_')) ?? false));

        foreach (var type in contributors)
            ((ISettingsContributor)Activator.CreateInstance(type)!).Register();
    }
}

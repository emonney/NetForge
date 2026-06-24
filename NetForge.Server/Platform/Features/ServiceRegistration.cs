using System.Reflection;

namespace NetForge.Server.Platform.Features;

/// <summary>
/// Implemented by a slice that needs to register its own services in DI (e.g. a feature service
/// like <c>INotificationService</c>, or a search provider). Discovered by reflection at startup —
/// the DI analogue of <c>IFeatureEndpoints</c> and <c>ISettingsContributor</c> — so a slice wires
/// its services without editing Program.cs. Underscore-prefixed namespaces are skipped.
/// </summary>
public interface IServiceRegistrar
{
    void Register(IServiceCollection services);
}

public static class ServiceRegistration
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        var registrars = typeof(IServiceRegistrar).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IServiceRegistrar).IsAssignableFrom(t))
            .Where(t => !(t.Namespace?.Split('.').Any(s => s.StartsWith('_')) ?? false))
            .OrderBy(t => t.Name);

        foreach (var type in registrars)
            ((IServiceRegistrar)Activator.CreateInstance(type)!).Register(services);

        return services;
    }
}

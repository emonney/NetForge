using System.Reflection;

namespace NetForge.Server.Platform.Features;

/// <summary>
/// Implemented by each vertical slice's endpoint class. Discovered by reflection at
/// startup via <see cref="FeatureRegistration.MapAllFeatures"/> — never wire a slice
/// into Program.cs by hand.
/// </summary>
public interface IFeatureEndpoints
{
    void Map(IEndpointRouteBuilder app);
}

/// <summary>Controls slice registration order; lower runs first, default 100.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class FeatureOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

public static class FeatureRegistration
{
    public static IEndpointRouteBuilder MapAllFeatures(this IEndpointRouteBuilder app)
    {
        var slices = typeof(IFeatureEndpoints).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IFeatureEndpoints).IsAssignableFrom(t))
            // Underscore-prefixed namespaces (e.g. _Template) are copy-source scaffolding, not live features.
            .Where(t => !IsTemplateNamespace(t.Namespace))
            .OrderBy(t => t.GetCustomAttribute<FeatureOrderAttribute>()?.Order ?? 100)
            .ThenBy(t => t.Name)
            .ToArray();

        foreach (var slice in slices)
            ((IFeatureEndpoints)Activator.CreateInstance(slice)!).Map(app);

        var logger = app.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FeatureRegistration");
        logger.LogInformation("Registered {Count} feature slice(s): {Slices}",
            slices.Length, slices.Length == 0 ? "(none)" : string.Join(", ", slices.Select(s => s.Name)));

        return app;
    }

    private static bool IsTemplateNamespace(string? ns) =>
        ns?.Split('.').Any(segment => segment.StartsWith('_')) ?? false;
}

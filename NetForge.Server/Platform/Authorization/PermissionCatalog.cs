using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace NetForge.Server.Platform.Authorization;

/// <summary>A single declarable permission: its dotted name, the feature group it belongs to, and a
/// human description for the admin catalog. The description comes from a <c>[Description]</c> on the
/// constant, falling back to a humanised action name.</summary>
public sealed record PermissionDescriptor(string Name, string Group, string Description);

/// <summary>Permissions for one feature, ready for the grouped admin UI.</summary>
public sealed record PermissionGroup(string Name, IReadOnlyList<PermissionDescriptor> Permissions);

/// <summary>
/// The set of every permission the app declares, aggregated once at startup by reflecting over
/// <c>public const string</c> fields of any <c>*Permissions</c> class — exactly the constants each
/// vertical slice owns. Underscore-prefixed namespaces (e.g. <c>_Template</c>) are skipped, mirroring
/// feature/EF discovery. Registered as a singleton; feeds both the policy validator and the catalog UI.
/// </summary>
public sealed class PermissionCatalog
{
    public IReadOnlyList<PermissionDescriptor> All { get; }
    public IReadOnlyList<PermissionGroup> Groups { get; }
    private readonly HashSet<string> _names;

    public PermissionCatalog(Assembly assembly)
    {
        All = Discover(assembly)
            .DistinctBy(p => p.Name)
            .OrderBy(p => p.Group, StringComparer.Ordinal)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        _names = All.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Groups = All
            .GroupBy(p => p.Group)
            .Select(g => new PermissionGroup(g.Key, g.ToArray()))
            .OrderBy(g => g.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>True for a name that's a declared permission or a wildcard the catalog can cover
    /// (<c>*</c>, or <c>group.*</c> for a known group). Role assignment validates against this.</summary>
    public bool IsAssignable(string permission)
    {
        if (permission == PermissionClaims.All) return true;
        if (_names.Contains(permission)) return true;
        if (!permission.EndsWith(".*", StringComparison.Ordinal)) return false;

        var group = permission[..^2];
        return Groups.Any(g => string.Equals(g.Name, group, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<PermissionDescriptor> Discover(Assembly assembly) =>
        from type in assembly.GetTypes()
        where type is { IsClass: true, IsAbstract: true, IsSealed: true } // static class
              && type.Name.EndsWith("Permissions", StringComparison.Ordinal)
              && !IsTemplateNamespace(type.Namespace)
        from field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
        where field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string)
        let name = (string)field.GetRawConstantValue()!
        select new PermissionDescriptor(name, GroupOf(name), DescriptionOf(field, name));

    private static string GroupOf(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    private static string DescriptionOf(FieldInfo field, string name)
    {
        if (field.GetCustomAttribute<DescriptionAttribute>()?.Description is { Length: > 0 } description)
            return description;

        // "users.read" → "Read users"; a readable default when no [Description] is supplied.
        var dot = name.IndexOf('.');
        if (dot < 0) return Capitalize(name);
        var group = name[..dot];
        var action = name[(dot + 1)..].Replace('.', ' ');
        return $"{Capitalize(action)} {group}";
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value[..1]) + value[1..];

    private static bool IsTemplateNamespace(string? ns) =>
        ns?.Split('.').Any(segment => segment.StartsWith('_')) ?? false;
}

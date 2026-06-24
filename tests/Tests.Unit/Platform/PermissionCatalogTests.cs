using System.Reflection;
using NetForge.Server.Features.Users;
using NetForge.Server.Platform.Authorization;
using Shouldly;

namespace NetForge.Tests.Unit.Platform;

/// <summary>
/// The catalog is reflected once at startup from every <c>*Permissions</c> class's <c>public const
/// string</c> fields. These tests pin the discovery (a real slice permission shows up, with its
/// <c>[Description]</c> and derived group) and the wildcard-aware <see cref="PermissionCatalog.IsAssignable"/>
/// that role assignment validates against.
/// </summary>
public class PermissionCatalogTests
{
    // Reflect the real server assembly — the same one the app uses at startup. Anchored on a core
    // (non-demo) slice's permission so this fact holds whether or not the Sales demo is included.
    private static readonly PermissionCatalog Catalog = new(typeof(UserPermissions).Assembly);

    [Fact]
    public void Discovers_slice_permission_constants_with_description_and_group()
    {
        var matches = Catalog.All.Where(c => c.Name == "users.read").ToList();

        matches.Count.ShouldBe(1); // discovered exactly once, however many endpoints reference it
        matches[0].Group.ShouldBe("users");          // prefix before the first dot
        matches[0].Description.ShouldBe("View users"); // from the [Description] attribute
    }

    [Fact]
    public void Excludes_underscore_prefixed_template_namespaces()
    {
        // Features/_Template constants must never leak into the live catalog.
        Catalog.All.ShouldNotContain(c => c.Name.StartsWith("template.", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("users.read")]      // exact declared permission
    [InlineData("users.*")]          // wildcard over a known group
    [InlineData("*")]                // superadmin
    public void IsAssignable_accepts_declared_permissions_and_known_wildcards(string permission) =>
        Catalog.IsAssignable(permission).ShouldBeTrue();

    [Theory]
    [InlineData("users.fly")]        // undeclared action
    [InlineData("nonsense.*")]       // wildcard over an unknown group
    [InlineData("totally.made.up")]  // undeclared permission
    public void IsAssignable_rejects_unknown_permissions(string permission) =>
        Catalog.IsAssignable(permission).ShouldBeFalse();
}

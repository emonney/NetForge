using NetForge.Server.Platform.Authorization;
using Shouldly;

namespace NetForge.Tests.Unit.Platform;

/// <summary>
/// Pure permission-matching rules: exact, the global <c>*</c> superadmin wildcard, and trailing
/// group wildcards (<c>users.*</c> grants <c>users.read</c>). These gate every endpoint, so the
/// matching semantics — including case-insensitivity — are worth pinning down explicitly.
/// </summary>
public class PermissionClaimsTests
{
    [Theory]
    [InlineData("products.read", "products.read")]   // exact
    [InlineData("*", "anything.at.all")]              // superadmin
    [InlineData("products.*", "products.read")]       // group wildcard
    [InlineData("products.*", "products.delete")]      // group wildcard, different action
    [InlineData("Products.Read", "products.read")]    // case-insensitive
    public void Grants_returns_true_when_granted_covers_required(string granted, string required) =>
        PermissionClaims.Grants(granted, required).ShouldBeTrue();

    [Theory]
    [InlineData("products.read", "products.delete")]  // different action
    [InlineData("products.*", "orders.read")]          // wrong group
    [InlineData("products.read", "products.read.all")] // narrower than required
    public void Grants_returns_false_when_granted_does_not_cover_required(string granted, string required) =>
        PermissionClaims.Grants(granted, required).ShouldBeFalse();

    [Fact]
    public void Satisfies_is_true_when_any_granted_permission_covers_the_requirement()
    {
        string[] granted = ["orders.read", "products.*", "users.read"];

        PermissionClaims.Satisfies(granted, "products.create").ShouldBeTrue();
        PermissionClaims.Satisfies(granted, "settings.update").ShouldBeFalse();
    }
}

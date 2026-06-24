using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NetForge.Server.Data;
using NetForge.Server.Features.Auth;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Tests.Integration.Fixtures;
using Shouldly;

namespace NetForge.Tests.Integration.Features.Auth;

/// <summary>
/// A self-registered user is granted the configured <c>Account.DefaultRole</c> (default "Member") in
/// the active tenant, so their first sign-in lands on a usable app instead of an empty shell. The
/// "Testing" environment skips the dev seeders, so the role is created here for the run.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class RegistrationDefaultRoleTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Registering_grants_the_default_role()
    {
        await EnsureMemberRoleAsync();

        var email = $"new-signup-{Guid.NewGuid():N}@netforge.test";
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Sign-upP@ss1", "New Signup"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var tenantRoles = scope.ServiceProvider.GetRequiredService<ITenantRoleService>();

        var user = await users.FindByEmailAsync(email);
        user.ShouldNotBeNull();

        var roleNames = await tenantRoles.RoleNamesAsync(user!.Id, TenancyOptions.DefaultTenant);
        roleNames.ShouldContain(SystemRoles.Member);
    }

    private async Task EnsureMemberRoleAsync()
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (await roles.FindByNameAsync(SystemRoles.Member) is not null) return;

        var role = new IdentityRole(SystemRoles.Member);
        (await roles.CreateAsync(role)).Succeeded.ShouldBeTrue();
        await roles.AddClaimAsync(role, new Claim(PermissionClaims.ClaimType, "products.read"));
    }
}

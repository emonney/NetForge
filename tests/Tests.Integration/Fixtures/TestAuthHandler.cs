using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Tests.Integration.Fixtures;

/// <summary>
/// Replaces the cookie scheme as the default authenticator for tests so we can mint a principal with
/// exactly the permission claims a scenario needs — without driving the full login/cookie/security-stamp
/// machinery (that path is covered separately by the auth-flow test). A request carries its identity in
/// headers: no <see cref="UserIdHeader"/> ⇒ anonymous (401 on protected routes); a comma-separated
/// <see cref="PermissionsHeader"/> ⇒ those <c>permission</c> claims, which the real
/// <see cref="PermissionAuthorizationHandler"/> then evaluates.
/// </summary>
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string PermissionsHeader = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId) || string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult()); // unauthenticated request

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId!),
            new(ClaimTypes.Name, userId!),
            new(TenantClaims.ClaimType, TenancyOptions.DefaultTenant),
        };

        if (Request.Headers.TryGetValue(PermissionsHeader, out var permissions))
            foreach (var permission in permissions.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(PermissionClaims.ClaimType, permission));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

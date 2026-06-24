using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NetForge.Server.Platform.Authorization;

/// <summary>
/// Materialises permission policies on demand so no one has to register a named policy per
/// permission. A policy name of the form <c>perm:users.read</c> (see <see cref="PermissionPolicy"/>)
/// becomes a one-requirement policy; everything else defers to the default provider so built-in
/// <c>[Authorize]</c> usage keeps working.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionPolicy.TryGetPermission(policyName, out var permission))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace NetForge.Server.Platform.Authorization;

/// <summary>
/// Bridges a permission string to an ASP.NET authorization policy name so endpoints can demand a
/// permission without anyone pre-registering a policy. <see cref="PermissionPolicyProvider"/>
/// recognises the <see cref="Prefix"/> and builds the matching policy on the fly.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(string permission) => Prefix + permission;

    public static bool TryGetPermission(string policyName, out string permission)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            permission = policyName[Prefix.Length..];
            return true;
        }

        permission = string.Empty;
        return false;
    }
}

/// <summary>Permission gates for minimal-API endpoints. Equivalent to <c>[HasPermission]</c> on
/// controllers; prefer this on slice groups/handlers (<c>group.RequirePermission(Xxx.Read)</c>).</summary>
public static class PermissionEndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(PermissionPolicy.Name(permission));
}

/// <summary>Declarative permission gate for controller actions / classes. Minimal-API slices use
/// <see cref="PermissionEndpointExtensions.RequirePermission{TBuilder}"/> instead.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) => Policy = PermissionPolicy.Name(permission);
}

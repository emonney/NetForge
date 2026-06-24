using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace NetForge.Server.Platform.Authorization;

/// <summary>
/// Wires permission-based authorization: the catalog (aggregated from slice <c>*Permissions</c>
/// constants), the on-demand policy provider, and the wildcard-aware handler. Replaces the plain
/// <c>AddAuthorization()</c> call in <see cref="Identity.IdentitySetup"/>.
/// </summary>
public static class AuthorizationSetup
{
    public static IServiceCollection AddPlatformAuthorization(this IServiceCollection services)
    {
        services.AddSingleton(new PermissionCatalog(Assembly.GetExecutingAssembly()));
        services.AddScoped<PermissionResolver>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        return services;
    }
}

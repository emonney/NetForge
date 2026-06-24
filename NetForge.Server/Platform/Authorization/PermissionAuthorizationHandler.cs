using Microsoft.AspNetCore.Authorization;

namespace NetForge.Server.Platform.Authorization;

/// <summary>Succeeds when any of the principal's permission claims grants the required permission,
/// honouring wildcard expansion (<c>*</c>, <c>users.*</c>). The provider already required an
/// authenticated user, so an anonymous request never reaches here.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var granted = context.User
            .FindAll(PermissionClaims.ClaimType)
            .Select(c => c.Value);

        if (PermissionClaims.Satisfies(granted, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

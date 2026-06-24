using Microsoft.AspNetCore.Authorization;

namespace NetForge.Server.Platform.Authorization;

/// <summary>Requires the principal to hold a permission that grants <paramref name="Permission"/>
/// (directly or via a wildcard). Built on demand by <see cref="PermissionPolicyProvider"/>.</summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Features;

namespace NetForge.Server.Features.Roles;

/// <summary>
/// Read-only catalog of every permission the app declares, grouped by feature, for the admin
/// catalog page and the role editor's permission picker. Reading roles implies reading the catalog,
/// so it shares the <see cref="RolePermissions.Read"/> gate.
/// </summary>
public sealed class PermissionCatalogEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/permissions", List)
            .WithTags("Roles")
            .RequirePermission(RolePermissions.Read);
    }

    private static IResult List(PermissionCatalog catalog) =>
        Results.Ok(catalog.Groups.Select(g => new
        {
            name = g.Name,
            permissions = g.Permissions.Select(p => new { name = p.Name, description = p.Description }),
        }));
}

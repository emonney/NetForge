using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;
using NetForge.Server.Platform.MultiTenancy;
using ValidationException = NetForge.Server.Platform.Errors.ValidationException;

namespace NetForge.Server.Features.Roles;

/// <summary>
/// Role management: roles group permissions, users get roles, effective permissions are the union.
/// Permissions are stored as role claims (type <see cref="PermissionClaims.ClaimType"/>) so they
/// flow onto every member's principal automatically. System roles (Admin) are read-only — the API
/// refuses to rename or delete them so the app can't be stripped of its superadmin.
/// </summary>
public sealed class RoleEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).RequirePermission(RolePermissions.Read);
        group.MapGet("/{id}", Get).RequirePermission(RolePermissions.Read);
        group.MapPost("/", Create).RequirePermission(RolePermissions.Create).AddEndpointFilter<TransactionFilter>();
        group.MapPut("/{id}", Update).RequirePermission(RolePermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id}", Delete).RequirePermission(RolePermissions.Delete).AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles, CancellationToken ct)
    {
        var all = roles.Roles.OrderBy(r => r.Name).ToList();
        var dtos = new List<RoleDto>(all.Count);
        foreach (var role in all)
            dtos.Add(await ToDtoAsync(role, roles, tenantRoles));

        return Results.Ok(dtos);
    }

    private static async Task<IResult> Get(string id, RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);
        return Results.Ok(await ToDtoAsync(role, roles, tenantRoles));
    }

    private static async Task<IResult> Create(
        SaveRoleRequest req, RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles)
    {
        var role = new IdentityRole(req.Name.Trim());
        var result = await roles.CreateAsync(role);
        if (!result.Succeeded) throw RoleErrors(result);

        await SyncPermissionsAsync(role, req.Permissions, roles);
        return Results.Created($"/api/roles/{role.Id}", await ToDtoAsync(role, roles, tenantRoles));
    }

    private static async Task<IResult> Update(
        string id, SaveRoleRequest req, RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);
        if (SystemRoles.IsSystem(role.Name!))
            throw new ForbiddenException($"The '{role.Name}' role is built in and can't be edited.");

        if (!string.Equals(role.Name, req.Name.Trim(), StringComparison.Ordinal))
        {
            var renamed = await roles.SetRoleNameAsync(role, req.Name.Trim());
            if (renamed.Succeeded) renamed = await roles.UpdateAsync(role);
            if (!renamed.Succeeded) throw RoleErrors(renamed);
        }

        await SyncPermissionsAsync(role, req.Permissions, roles);
        return Results.Ok(await ToDtoAsync(role, roles, tenantRoles));
    }

    private static async Task<IResult> Delete(string id, RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);
        if (SystemRoles.IsSystem(role.Name!))
            throw new ForbiddenException($"The '{role.Name}' role is built in and can't be deleted.");

        // Drop the role's per-tenant grants first so no TenantUserRole rows are orphaned.
        await tenantRoles.RemoveRoleEverywhereAsync(role.Id);

        var result = await roles.DeleteAsync(role);
        if (!result.Succeeded) throw RoleErrors(result);

        return Results.NoContent();
    }

    // Reconcile the role's permission claims to exactly the requested set (add the new, drop the gone).
    private static async Task SyncPermissionsAsync(
        IdentityRole role, IReadOnlyList<string> desired, RoleManager<IdentityRole> roles)
    {
        var current = (await roles.GetClaimsAsync(role))
            .Where(c => c.Type == PermissionClaims.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var target = desired.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var add in target.Where(p => !current.Contains(p)))
            await roles.AddClaimAsync(role, new Claim(PermissionClaims.ClaimType, add));

        foreach (var remove in current.Where(p => !target.Contains(p)))
            await roles.RemoveClaimAsync(role, new Claim(PermissionClaims.ClaimType, remove));
    }

    private static async Task<RoleDto> ToDtoAsync(
        IdentityRole role, RoleManager<IdentityRole> roles, ITenantRoleService tenantRoles)
    {
        var permissions = (await roles.GetClaimsAsync(role))
            .Where(c => c.Type == PermissionClaims.ClaimType)
            .Select(c => c.Value)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        // Members = distinct users granted this role across all tenants (per-tenant assignment table).
        var userCount = await tenantRoles.CountUsersInRoleAsync(role.Id);

        return new RoleDto(role.Id, role.Name!, SystemRoles.IsSystem(role.Name!), permissions, userCount);
    }

    private static ValidationException RoleErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        // Duplicate name is the common, user-actionable case — surface it on the name field.
        var field = result.Errors.Any(e => e.Code == "DuplicateRoleName") ? "name" : string.Empty;
        return new ValidationException(new Dictionary<string, string[]> { [field] = messages });
    }
}

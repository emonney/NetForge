namespace NetForge.Server.Features.Roles;

/// <summary>A role and the permissions it grants. <see cref="IsSystem"/> roles (e.g. Admin) are
/// read-only in the UI — they can't be renamed or deleted.</summary>
public record RoleDto(
    string Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UserCount);

/// <summary>Create or replace a role: its name plus the full set of permissions it should grant
/// (each must be assignable per the catalog — a declared permission or a <c>group.*</c>/<c>*</c>
/// wildcard).</summary>
public record SaveRoleRequest(string Name, IReadOnlyList<string> Permissions);

namespace NetForge.Server.Features.Users;

/// <summary>A user as the admin list/detail renders them. <see cref="IsSelf"/> flags the requesting
/// admin's own row so the UI can disable self-targeting actions (lock/delete/role changes).</summary>
public record UserDto(
    string Id,
    string Email,
    string? DisplayName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool LockedOut,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    bool IsSelf);

/// <summary>Replace a user's roles with exactly this set. Each name must be an existing role.</summary>
public record UpdateUserRolesRequest(IReadOnlyList<string> Roles);

/// <summary>Admin edit of a user's basic identity. Changing <see cref="Email"/> also moves the username
/// and clears email confirmation (the new address is unverified until re-confirmed or admin-vouched).</summary>
public record UpdateUserRequest(string? DisplayName, string Email);

/// <summary>
/// Admin-provision a new user. Invite-first: with no <see cref="Password"/> and <see cref="SendInvite"/>
/// set, the account is created password-less and emailed a "set your password" link (the reset flow);
/// supply a <see cref="Password"/> instead to set a temporary one directly. <see cref="EmailConfirmed"/>
/// lets the admin vouch for the address so the user can sign in without the confirmation step.
/// </summary>
public record CreateUserRequest(
    string Email,
    string? DisplayName,
    IReadOnlyList<string>? Roles,
    bool EmailConfirmed,
    bool SendInvite,
    string? Password);

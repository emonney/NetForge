using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetForge.Server.Data;
using NetForge.Server.Features.Auth;
using NetForge.Server.Platform;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Email;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.Pagination;
using NetForge.Server.Platform.Settings;
using NetForge.Server.Platform.Webhooks;

namespace NetForge.Server.Features.Users;

/// <summary>
/// User administration: list/search, role assignment, lock/unlock, delete. Every state-changing
/// action refuses to target the requesting admin's own account — you can't lock, delete, or
/// de-role yourself into a lockout. Locking rotates the security stamp so live sessions die within
/// the cookie's 1-minute validation window.
/// </summary>
public sealed class UserEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).RequirePermission(UserPermissions.Read);
        group.MapGet("/{id}", Get).RequirePermission(UserPermissions.Read);
        group.MapPost("/", Create).RequirePermission(UserPermissions.Create).AddEndpointFilter<TransactionFilter>();
        group.MapPut("/{id}", UpdateUser).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapPut("/{id}/roles", UpdateRoles).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapPost("/{id}/confirm-email", ConfirmEmail).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapPost("/{id}/resend-confirmation", ResendConfirmation).RequirePermission(UserPermissions.Update);
        group.MapPost("/{id}/send-password-reset", SendPasswordReset).RequirePermission(UserPermissions.Update);
        group.MapPost("/{id}/disable-2fa", DisableTwoFactor).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapPost("/{id}/lock", Lock).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapPost("/{id}/unlock", Unlock).RequirePermission(UserPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id}", Delete).RequirePermission(UserPermissions.Delete).AddEndpointFilter<TransactionFilter>();
    }

    // Allowlist of what the DataGrid may sort/filter/search on — anything else in the query string
    // is ignored. createdAt sorts in SQL now that SQLite stores DateTimeOffset as ticks.
    private static QuerySpec<AppUser> UserQuerySpec() => new QuerySpec<AppUser>()
        .Allow("email", u => u.Email)
        .Allow("displayName", u => u.DisplayName)
        .Allow("createdAt", u => u.CreatedAt)
        .FilterOnly("emailConfirmed", u => u.EmailConfirmed)
        .FilterOnly("twoFactorEnabled", u => u.TwoFactorEnabled)
        .Searchable(u => u.Email)
        .Searchable(u => u.DisplayName)
        .DefaultSort("createdAt", descending: true);

    private static async Task<IResult> List(
        PagedRequest request, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles,
        HttpContext http, CancellationToken ct)
    {
        var self = SelfId(http);

        // Page + sort + filter the users in SQL; role names need a per-user lookup, so enrich the
        // page (≤ pageSize rows) afterward rather than N+1-ing the whole table.
        var paged = await users.Users.AsNoTracking().ToPagedResultAsync(request, UserQuerySpec(), u => u, ct);

        var now = DateTimeOffset.UtcNow;
        var dtos = new List<UserDto>(paged.Items.Count);
        foreach (var user in paged.Items)
            dtos.Add(await ToDtoAsync(user, tenantRoles, tenant.TenantId, self, now));

        return Results.Ok(PagedResult<UserDto>.Create(dtos, paged.Page, paged.PageSize, paged.TotalItems));
    }


    private static async Task<IResult> Get(
        string id, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles, HttpContext http)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> Create(
        CreateUserRequest req, UserManager<AppUser> users, RoleManager<IdentityRole> roles, IEmailSender email,
        ISettingService settings, ITenantContext tenant, ITenantRoleService tenantRoles, IEventBus bus,
        IOptions<AppOptions> appOptions, HttpContext http, CancellationToken ct)
    {
        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim(),
            EmailConfirmed = req.EmailConfirmed,
        };

        var hasPassword = !string.IsNullOrEmpty(req.Password);
        Throw(hasPassword ? await users.CreateAsync(user, req.Password!) : await users.CreateAsync(user));

        // Roles: an explicit set if given, otherwise the configured default role (same as self-registration).
        if (req.Roles is { Count: > 0 })
        {
            var roleIds = new List<string>(req.Roles.Count);
            foreach (var name in req.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await roles.FindByNameAsync(name)
                    ?? throw new BadRequestException($"Role '{name}' does not exist.");
                roleIds.Add(role.Id);
            }
            await tenantRoles.SetRoleIdsAsync(user.Id, tenant.TenantId, roleIds);
        }
        else
        {
            await AuthEndpoints.AssignDefaultRoleAsync(user, settings, roles, tenantRoles, tenant, ct);
        }

        // Invite: email a "set your password" link (the reset-password flow). Skipped when a temp password was set.
        if (req.SendInvite && !hasPassword)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            await AuthEmails.SendPasswordResetAsync(
                email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value),
                appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);
        }

        await bus.PublishAsync(UserWebhookEvents.Created, new { userId = user.Id, user.Email });
        var dto = await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow);
        return Results.Created($"/api/users/{user.Id}", dto);
    }

    private static async Task<IResult> UpdateUser(
        string id, UpdateUserRequest req, UserManager<AppUser> users,
        ITenantContext tenant, ITenantRoleService tenantRoles, HttpContext http)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);

        user.DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim();

        var newEmail = req.Email.Trim();
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            // Changing the email moves the username too (the app keeps them equal) and re-requires
            // confirmation (SetEmailAsync clears EmailConfirmed) — the admin can re-verify if they vouch.
            Throw(await users.SetUserNameAsync(user, newEmail));
            Throw(await users.SetEmailAsync(user, newEmail));
        }
        Throw(await users.UpdateAsync(user));

        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    // Admin vouches for an address — flip EmailConfirmed so the user can sign in without the confirm step.
    private static async Task<IResult> ConfirmEmail(
        string id, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles, HttpContext http)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            Throw(await users.UpdateAsync(user));
        }
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    // Re-send the email-confirmation link to a user still pending confirmation (the admin counterpart to
    // the anonymous self-service resend). For an already-invited account use Send password reset instead.
    private static async Task<IResult> ResendConfirmation(
        string id, UserManager<AppUser> users, IEmailSender email, IOptions<AppOptions> appOptions,
        HttpContext http, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        if (user.EmailConfirmed) throw new BadRequestException("This user's email is already confirmed.");

        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await AuthEmails.SendEmailConfirmationAsync(
            email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value),
            appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);
        return Results.Ok(new { message = "A confirmation link has been sent." });
    }

    // Unblock a user locked out of their authenticator: turn 2FA off and discard the old secret so they
    // can re-enroll from scratch. Uses only core Identity APIs, so it compiles even in editions without 2FA
    // (where no account ever has it enabled, the admin UI simply never offers this).
    private static async Task<IResult> DisableTwoFactor(
        string id, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles, HttpContext http)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        if (user.TwoFactorEnabled)
        {
            Throw(await users.SetTwoFactorEnabledAsync(user, false));
            await users.ResetAuthenticatorKeyAsync(user);
        }
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    // Admin-triggered reset: emails the same single-use link as self-service "forgot password" (the
    // admin never sees the password). The user picks a new one via the standard /reset-password page.
    // Rotating the security stamp first revokes the user's live sessions (they die within the cookie's
    // 1-minute validation window) — hygiene for a possibly-compromised account — and must come BEFORE the
    // token is minted, since the reset token embeds the stamp.
    private static async Task<IResult> SendPasswordReset(
        string id, UserManager<AppUser> users, IEmailSender email, IOptions<AppOptions> appOptions,
        HttpContext http, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        await users.UpdateSecurityStampAsync(user);
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await AuthEmails.SendPasswordResetAsync(
            email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value),
            appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);
        return Results.Ok(new { message = "A password reset link has been sent." });
    }

    private static async Task<IResult> UpdateRoles(
        string id, UpdateUserRolesRequest req, UserManager<AppUser> users, RoleManager<IdentityRole> roles,
        ITenantContext tenant, ITenantRoleService tenantRoles, IEventBus bus, HttpContext http)
    {
        var user = await RequireOther(id, users, http, "change your own roles");

        var target = req.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var roleIds = new List<string>(target.Length);
        foreach (var name in target)
        {
            var role = await roles.FindByNameAsync(name)
                ?? throw new BadRequestException($"Role '{name}' does not exist.");
            roleIds.Add(role.Id);
        }

        // Role assignment is per-tenant: write to TenantUserRole for the current tenant (the "default"
        // tenant in single-tenant mode), not the global AspNetUserRoles.
        await tenantRoles.SetRoleIdsAsync(user.Id, tenant.TenantId, roleIds);

        // Roles changed → permissions changed; rotate the stamp so the member's principal refreshes.
        await users.UpdateSecurityStampAsync(user);
        await bus.PublishAsync(UserWebhookEvents.RolesChanged, new { userId = user.Id, user.Email, roles = target });
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> Lock(
        string id, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles,
        IEventBus bus, HttpContext http)
    {
        var user = await RequireOther(id, users, http, "lock your own account");

        await users.SetLockoutEnabledAsync(user, true);
        Throw(await users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));
        await users.UpdateSecurityStampAsync(user); // kill live sessions within the validation window

        await bus.PublishAsync(UserWebhookEvents.Locked, new { userId = user.Id, user.Email });
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> Unlock(
        string id, UserManager<AppUser> users, ITenantContext tenant, ITenantRoleService tenantRoles,
        IEventBus bus, HttpContext http)
    {
        var user = await RequireOther(id, users, http, "unlock your own account");

        Throw(await users.SetLockoutEndDateAsync(user, null));
        await users.ResetAccessFailedCountAsync(user);

        await bus.PublishAsync(UserWebhookEvents.Unlocked, new { userId = user.Id, user.Email });
        return Results.Ok(await ToDtoAsync(user, tenantRoles, tenant.TenantId, SelfId(http), DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> Delete(string id, UserManager<AppUser> users, IEventBus bus, HttpContext http)
    {
        var user = await RequireOther(id, users, http, "delete your own account");

        // Capture identity before the row is gone — the event still needs to say who was deleted.
        var (deletedId, deletedEmail) = (user.Id, user.Email);
        Throw(await users.DeleteAsync(user));

        await bus.PublishAsync(UserWebhookEvents.Deleted, new { userId = deletedId, email = deletedEmail });
        return Results.NoContent();
    }

    // Loads the target and guarantees it isn't the caller — the guard behind every mutating action.
    private static async Task<AppUser> RequireOther(string id, UserManager<AppUser> users, HttpContext http, string action)
    {
        var user = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        if (user.Id == SelfId(http)) throw new ForbiddenException($"You can't {action}.");
        return user;
    }

    private static async Task<UserDto> ToDtoAsync(
        AppUser user, ITenantRoleService tenantRoles, string tenantId, string? self, DateTimeOffset now)
    {
        var roles = (await tenantRoles.RoleNamesAsync(user.Id, tenantId)).ToArray();
        var lockedOut = user.LockoutEnd is { } end && end > now;
        return new UserDto(
            user.Id, user.Email ?? string.Empty, user.DisplayName, user.AvatarUrl, user.EmailConfirmed,
            user.TwoFactorEnabled, lockedOut, roles, user.CreatedAt, user.Id == self);
    }

    private static string? SelfId(HttpContext http) => http.User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static void Throw(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

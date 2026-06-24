using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NetForge.Server.Data;
using NetForge.Server.Platform;
using NetForge.Server.Platform.Auditing;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Email;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;
using NetForge.Server.Platform.Identity;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.RateLimiting;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Auth;

/// <summary>
/// Cookie-based auth flows: register → confirm → login → forgot/reset → change password, plus
/// /me for the SPA to bootstrap its session. 2FA and OAuth endpoints live in sibling files of
/// this slice. Auth failures throw typed <see cref="DomainException"/>s mapped to ProblemDetails.
/// </summary>
public sealed class AuthEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        // Unauthenticated credential endpoints carry a strict per-IP rate limit on top — a blunt
        // brute-force / account-enumeration throttle. /me + the authenticated writes are exempt
        // (the SPA polls /me) and rely on the generous global /api net instead.
        var credentials = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>()
            .RequireRateLimiting(RateLimitSetup.Auth);

        credentials.MapPost("/register", Register);
        credentials.MapPost("/confirm-email", ConfirmEmail);
        credentials.MapPost("/resend-confirmation", ResendConfirmation);
        credentials.MapPost("/login", Login);
        credentials.MapPost("/forgot-password", ForgotPassword);
        credentials.MapPost("/reset-password", ResetPassword);

        group.MapPost("/logout", Logout);
        group.MapGet("/public-config", PublicConfig);

        group.MapGet("/me", Me).RequireAuthorization();
        group.MapPut("/profile", UpdateProfile).RequireAuthorization();
        group.MapPut("/preferences", UpdatePreferences).RequireAuthorization();
        group.MapPost("/change-password", ChangePassword).RequireAuthorization();
    }

    // Anonymous bootstrap for the sign-in / register screens: whether self-service registration is open (the SPA
    // hides the "Create one" link and the register form when it isn't — the Register handler also enforces it),
    // plus optional shared demo credentials ("App:DemoLogin") the login screen offers as a click-to-fill chip.
    private static async Task<IResult> PublicConfig(ISettingService settings, IOptions<AppOptions> appOptions, CancellationToken ct)
    {
        var demo = appOptions.Value.DemoLogin;
        object? demoLogin = !string.IsNullOrWhiteSpace(demo?.Email) && !string.IsNullOrWhiteSpace(demo?.Password)
            ? new { email = demo!.Email!.Trim(), password = demo.Password }
            : null;
        return Results.Ok(new
        {
            allowRegistration = await settings.GetAsync<bool>(AccountSettings.AllowRegistration, ct),
            demoLogin,
        });
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest req, UserManager<AppUser> users, ITenantRoleService tenantRoles,
        PermissionResolver permissions, HttpContext http)
    {
        var user = await users.GetUserAsync(http.User);
        if (user is null) return Results.Unauthorized();

        user.DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw IdentityErrors(result);

        return Results.Ok(await user.ToAuthDtoAsync(tenantRoles, permissions));
    }

    private static async Task<IResult> UpdatePreferences(
        UpdatePreferencesRequest req, UserManager<AppUser> users, ITenantRoleService tenantRoles,
        PermissionResolver permissions, HttpContext http)
    {
        var user = await users.GetUserAsync(http.User);
        if (user is null) return Results.Unauthorized();

        user.Locale = string.IsNullOrWhiteSpace(req.Locale) ? null : req.Locale.Trim();
        user.TimeZone = string.IsNullOrWhiteSpace(req.TimeZone) ? null : req.TimeZone.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw IdentityErrors(result);

        return Results.Ok(await user.ToAuthDtoAsync(tenantRoles, permissions));
    }

    private static async Task<IResult> Register(
        RegisterRequest req, UserManager<AppUser> users, RoleManager<IdentityRole> roles, IEmailSender email,
        ISettingService settings, ITenantRoleService tenantRoles, ITenantContext tenant,
        IOptions<AppOptions> appOptions, HttpContext http, CancellationToken ct)
    {
        if (await settings.GetAsync<bool>(AccountSettings.AllowRegistration, ct) is false)
            throw new ForbiddenException("Registration is currently disabled. Contact an administrator for an invitation.");

        var user = new AppUser { UserName = req.Email, Email = req.Email, DisplayName = req.DisplayName };
        var result = await users.CreateAsync(user, req.Password);
        if (!result.Succeeded) throw IdentityErrors(result);

        await AssignDefaultRoleAsync(user, settings, roles, tenantRoles, tenant, ct);

        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await AuthEmails.SendEmailConfirmationAsync(
            email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value), appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);

        return Results.Ok(new { message = "Account created. Check your email to confirm your address." });
    }

    // Grants the configured Account.DefaultRole to a brand-new account so the first sign-in shows a
    // usable app, not an empty shell. Resolved by name in the active tenant (the "default" tenant in
    // single-tenant mode); a blank or unknown role name is a no-op. No security-stamp refresh is needed
    // — the principal is built fresh when the user confirms their email and signs in.
    internal static async Task AssignDefaultRoleAsync(
        AppUser user, ISettingService settings, RoleManager<IdentityRole> roles,
        ITenantRoleService tenantRoles, ITenantContext tenant, CancellationToken ct)
    {
        var roleName = await settings.GetAsync<string>(AccountSettings.DefaultRole, ct);
        if (string.IsNullOrWhiteSpace(roleName)) return;

        var role = await roles.FindByNameAsync(roleName);
        if (role is not null)
            await tenantRoles.GrantRoleAsync(user.Id, tenant.TenantId, role.Id, ct);
    }

    private static async Task<IResult> ConfirmEmail(ConfirmEmailRequest req, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(req.UserId);
        if (user is null) throw new BadRequestException("Invalid confirmation link.");

        // Idempotent on purpose: the confirmation token is single-use, but the link can legitimately be
        // hit more than once — a double-click, the SPA re-firing on remount, a browser/email/AV
        // prefetcher, back/forward. The first hit confirms and consumes the token; a later hit would
        // then fail token validation even though the account is fine. So if it's already confirmed,
        // succeed — otherwise the user is stuck on a "confirming…"/error screen for an account that's
        // actually ready to sign in.
        if (user.EmailConfirmed)
            return Results.Ok(new { message = "Email confirmed. You can now sign in." });

        var result = await users.ConfirmEmailAsync(user, Decode(req.Token));
        if (!result.Succeeded) throw new BadRequestException("Invalid or expired confirmation link.");

        return Results.Ok(new { message = "Email confirmed. You can now sign in." });
    }

    private static async Task<IResult> ResendConfirmation(
        ResendConfirmationRequest req, UserManager<AppUser> users, IEmailSender email,
        IOptions<AppOptions> appOptions, HttpContext http, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is { EmailConfirmed: false })
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            await AuthEmails.SendEmailConfirmationAsync(
                email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value), appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);
        }

        // Never reveal whether the email exists or its confirmation state.
        return Results.Ok(new { message = "If that address needs confirmation, a new link is on its way." });
    }

    private static async Task<IResult> Login(
        LoginRequest req, SignInManager<AppUser> signIn, UserManager<AppUser> users, ITenantRoleService tenantRoles,
        PermissionResolver permissions,
        IAuditService audit, HttpContext http, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null) throw new UnauthorizedException("Invalid email or password.", "INVALID_CREDENTIALS");

        var result = await signIn.PasswordSignInAsync(user, req.Password, req.RememberMe, lockoutOnFailure: true);

        if (result.RequiresTwoFactor) return Results.Ok(new LoginResultDto(RequiresTwoFactor: true, User: null));
        if (result.IsLockedOut) throw new UnauthorizedException("This account is temporarily locked. Try again later.", "ACCOUNT_LOCKED");
        if (result.IsNotAllowed) throw new UnauthorizedException("Confirm your email before signing in.", "EMAIL_NOT_CONFIRMED");
        if (!result.Succeeded) throw new UnauthorizedException("Invalid email or password.", "INVALID_CREDENTIALS");

        await audit.LogAsync("Auth", "Login", "AppUser", user.Id, cancellationToken: ct);
        return Results.Ok(new LoginResultDto(RequiresTwoFactor: false, User: await user.ToAuthDtoAsync(tenantRoles, permissions)));
    }

    private static async Task<IResult> Logout(SignInManager<AppUser> signIn, IAuditService audit, HttpContext http, CancellationToken ct)
    {
        // Capture the id before sign-out so the logout attaches to the user's timeline (entityType AppUser).
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await audit.LogAsync("Auth", "Logout", "AppUser", userId, cancellationToken: ct);
        await signIn.SignOutAsync();
        return Results.Ok();
    }

    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest req, UserManager<AppUser> users, IEmailSender email,
        IOptions<AppOptions> appOptions, HttpContext http, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is not null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            await AuthEmails.SendPasswordResetAsync(
                email, user, token, AuthUrls.ClientBaseUrl(http, appOptions.Value), appOptions.Value.ProductName, appOptions.Value.BrandColor, ct);
        }

        // Constant response regardless of existence — no account enumeration.
        return Results.Ok(new { message = "If an account exists for that email, a reset link is on its way." });
    }

    private static async Task<IResult> ResetPassword(ResetPasswordRequest req, UserManager<AppUser> users)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null) throw new BadRequestException("Invalid or expired reset link.");

        var result = await users.ResetPasswordAsync(user, Decode(req.Token), req.NewPassword);
        if (!result.Succeeded) throw IdentityErrors(result, passwordField: "newPassword");

        return Results.Ok(new { message = "Your password has been reset. You can now sign in." });
    }

    private static async Task<IResult> Me(
        UserManager<AppUser> users, ITenantRoleService tenantRoles, PermissionResolver permissions, HttpContext http)
    {
        var user = await users.GetUserAsync(http.User);
        return user is null ? Results.Unauthorized() : Results.Ok(await user.ToAuthDtoAsync(tenantRoles, permissions));
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest req, UserManager<AppUser> users, SignInManager<AppUser> signIn,
        IAuditService audit, HttpContext http, CancellationToken ct)
    {
        var user = await users.GetUserAsync(http.User);
        if (user is null) return Results.Unauthorized();

        // An OAuth-only account has no password yet — set the first one (there's no current password to verify).
        var hadPassword = await users.HasPasswordAsync(user);
        var result = hadPassword
            ? await users.ChangePasswordAsync(user, req.CurrentPassword ?? string.Empty, req.NewPassword)
            : await users.AddPasswordAsync(user, req.NewPassword);
        if (!result.Succeeded) throw IdentityErrors(result, passwordField: "newPassword");

        // Setting/changing the password rotates the security stamp; refresh the cookie so this session
        // isn't invalidated within the 1-minute validation window.
        await signIn.RefreshSignInAsync(user);
        await audit.LogAsync("Auth", hadPassword ? "PasswordChanged" : "PasswordSet", "AppUser", user.Id, cancellationToken: ct);

        return Results.Ok(new { message = hadPassword ? "Password changed." : "Password set." });
    }

    private static string Decode(string encoded)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded)); }
        catch (FormatException) { throw new BadRequestException("Invalid or malformed token."); }
    }

    // Best-effort mapping of Identity's error codes onto form fields so the SPA can show errors
    // inline. PasswordMismatch (wrong current password) → currentPassword; other Password* →
    // the caller's password field; duplicate/invalid email → email; anything else → form-level.
    private static ValidationException IdentityErrors(IdentityResult result, string passwordField = "password")
    {
        var byField = new Dictionary<string, List<string>>();
        foreach (var error in result.Errors)
        {
            var field = error.Code switch
            {
                "PasswordMismatch" => "currentPassword",
                _ when error.Code.StartsWith("Password", StringComparison.Ordinal) => passwordField,
                "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" => "email",
                _ => string.Empty,
            };
            if (!byField.TryGetValue(field, out var list)) byField[field] = list = [];
            list.Add(error.Description);
        }

        return new ValidationException(byField.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
    }
}

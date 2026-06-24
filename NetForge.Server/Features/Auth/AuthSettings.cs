using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Auth;

/// <summary>Keys for account/auth settings. App-scoped — an administrator configures them once for
/// the whole instance from <c>/admin/settings</c>.</summary>
public static class AccountSettings
{
    public const string Category = "Account";

    /// <summary>When false, self-service registration is rejected (invite-only).</summary>
    public const string AllowRegistration = "Account.AllowRegistration";

    /// <summary>Shown to users who need help; surfaced in emails/footers.</summary>
    public const string SupportEmail = "Account.SupportEmail";

    /// <summary>Role (by name) granted to a user on self-registration, so a first sign-in lands on a
    /// usable app instead of an empty shell. Blank disables the grant. The role must exist; if it was
    /// renamed or removed, no role is granted (a no-op, not an error).</summary>
    public const string DefaultRole = "Account.DefaultRole";
}

/// <summary>Registers the account settings into the catalog so they render in the admin settings UI
/// and resolve through <see cref="ISettingService"/>.</summary>
public sealed class AccountSettingsContributor : ISettingsContributor
{
    public void Register()
    {
        SettingDefinitions.Register(AccountSettings.AllowRegistration, typeof(bool), [SettingScope.App], true, AccountSettings.Category);
        SettingDefinitions.Register(AccountSettings.SupportEmail, typeof(string), [SettingScope.App], string.Empty, AccountSettings.Category);
        // Role-valued: renders as a dropdown of live roles (the "roles" ISettingOptionsProvider).
        SettingDefinitions.Register(
            AccountSettings.DefaultRole, typeof(string), [SettingScope.App], SystemRoles.Member,
            AccountSettings.Category, optionsProvider: "roles");
    }
}

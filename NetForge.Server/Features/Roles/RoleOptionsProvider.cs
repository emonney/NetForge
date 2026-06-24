using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Roles;

/// <summary>
/// Supplies the live roles as dropdown options for role-valued settings (e.g. <c>Account.DefaultRole</c>),
/// keyed "roles". Reflection-discovered, so a role-valued setting just declares <c>optionsProvider: "roles"</c>.
/// </summary>
public sealed class RoleOptionsProvider(RoleManager<IdentityRole> roles) : ISettingOptionsProvider
{
    public string Key => "roles";

    public async Task<IReadOnlyList<SettingOption>> GetOptionsAsync(CancellationToken ct)
    {
        var names = await roles.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync(ct);
        // A blank option lets an admin clear the setting (e.g. grant no default role).
        return new List<SettingOption> { new("", "— none —") }
            .Concat(names.Select(n => new SettingOption(n, n)))
            .ToList();
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Appearance;

/// <summary>The instance appearance the SPA applies (curated theme + optional accent + optional custom palette).</summary>
public sealed record AppearanceDto(string? Theme, string? BrandColor, string? CustomTheme);

public sealed record UpdateAppearanceRequest(string? Theme, string? BrandColor, string? CustomTheme);

/// <summary>
/// Instance appearance — the brand accent colour. <b>Read is anonymous</b> so the whole app, including the
/// pre-auth screens (login, etc.), picks up the brand; only an admin holding <c>appearance.manage</c> can
/// change it. Persisted App-scoped via <see cref="ISettingService"/>; the SPA re-tints <c>--primary</c> from it.
/// </summary>
public sealed class AppearanceEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appearance").WithTags("Appearance");
        group.MapGet("/", Get).AllowAnonymous();
    }

    private static async Task<IResult> Get(
        ISettingService settings, ITenantContext tenant, AppDbContext db, CancellationToken ct)
    {
        var theme = await settings.GetAsync<string>(AppearanceSettings.Theme, ct);
        var color = await settings.GetAsync<string>(AppearanceSettings.BrandColor, ct);
        var custom = await settings.GetAsync<string>(AppearanceSettings.CustomTheme, ct);

        // Fall back the accent to the active tenant's brand colour when no explicit accent is set, so per-tenant
        // branding flows through this single applier (with correct precedence and no inline override fighting
        // the themed tokens). Single-tenant resolves the "default" tenant, which has no colour → no change.
        if (string.IsNullOrWhiteSpace(color))
            color = await db.Set<Tenant>().Where(t => t.Id == tenant.TenantId)
                .Select(t => t.PrimaryColor).FirstOrDefaultAsync(ct);

        return Results.Ok(new AppearanceDto(Normalize(theme), Normalize(color), Normalize(custom)));
    }

    private static async Task<IResult> Update(
        UpdateAppearanceRequest req, ISettingService settings, ITenantContext tenant, CancellationToken ct)
    {
        var theme = (req.Theme ?? string.Empty).Trim();
        var color = (req.BrandColor ?? string.Empty).Trim();
        var custom = ValidateCustomTheme(req.CustomTheme);
        // A theme key is a short slug; the colour is injected into a CSS custom property on the client.
        // Browsers ignore an invalid custom-property value (no breakout), but reject characters that have no
        // place in a theme key or a colour token anyway.
        if (theme.Length > 32 || !IsSafe(theme) || color.Length > 64 || !IsSafe(color))
            throw new BadRequestException("Invalid appearance value.");

        // Save against the active tenant so each tenant keeps its own look (single-tenant → the "default" tenant).
        await settings.SetAsync(AppearanceSettings.Theme, theme, SettingScope.Tenant, tenant.TenantId, ct);
        await settings.SetAsync(AppearanceSettings.BrandColor, color, SettingScope.Tenant, tenant.TenantId, ct);
        await settings.SetAsync(AppearanceSettings.CustomTheme, custom, SettingScope.Tenant, tenant.TenantId, ct);
        return Results.Ok(new AppearanceDto(Normalize(theme), Normalize(color), Normalize(custom)));
    }

    private static bool IsSafe(string value) => value.IndexOfAny([';', '{', '}', '<', '>', '"', '\'', '\n', '\r']) < 0;

    /// <summary>The custom palette is a JSON blob the client builds; cap it and confirm it parses (each colour
    /// inside is re-validated client-side before being applied to a CSS custom property).</summary>
    private static string ValidateCustomTheme(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0) return string.Empty;
        if (v.Length > 4000) throw new BadRequestException("Custom theme is too large.");
        try { using var _ = JsonDocument.Parse(v); }
        catch { throw new BadRequestException("Custom theme must be valid JSON."); }
        return v;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

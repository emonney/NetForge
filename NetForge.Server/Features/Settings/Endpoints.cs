using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Settings;

/// <summary>
/// Admin configuration of App-scoped settings. The catalog is aggregated from each feature's
/// <see cref="ISettingsContributor"/>, so this slice never hard-codes which settings exist — new
/// ones appear automatically. User-scoped preferences are edited on the profile, not here.
/// </summary>
public sealed class SettingEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).RequirePermission(SettingPermissions.Read);
        group.MapPut("/{key}", Update).RequirePermission(SettingPermissions.Update).AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(
        AppDbContext db, IEnumerable<ISettingOptionsProvider> optionProviders, CancellationToken ct)
    {
        var definitions = SettingDefinitions.All.Where(d => d.Scopes.Contains(SettingScope.App)).ToList();

        var stored = await db.Set<Setting>().AsNoTracking()
            .Where(s => s.Scope == SettingScope.App && s.ScopeId == null)
            .ToDictionaryAsync(s => s.Key, s => s.ValueJson, StringComparer.OrdinalIgnoreCase, ct);

        // Resolve each distinct dynamic options provider once (e.g. "roles").
        var resolved = new Dictionary<string, IReadOnlyList<SettingOption>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in definitions.Where(d => d.OptionsProvider is not null).Select(d => d.OptionsProvider!).Distinct(StringComparer.OrdinalIgnoreCase))
            if (optionProviders.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) is { } provider)
                resolved[key] = await provider.GetOptionsAsync(ct);

        IReadOnlyList<SettingOption>? OptionsFor(SettingDefinition d) =>
            d.Options ?? (d.OptionsProvider is { } k && resolved.TryGetValue(k, out var o) ? o : null);

        var groups = definitions
            .Select(d =>
            {
                var defaultValue = JsonSerializer.SerializeToElement(d.DefaultValue);
                var value = stored.TryGetValue(d.Key, out var json) ? ParseElement(json) : defaultValue;
                var options = OptionsFor(d);
                return new SettingDto(d.Key, d.Category, options is not null ? "choice" : KindOf(d.Type), value, defaultValue, options);
            })
            .GroupBy(d => d.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new SettingCategoryDto(g.Key, g.OrderBy(s => s.Key, StringComparer.Ordinal).ToList()))
            .ToList();

        return Results.Ok(groups);
    }

    private static async Task<IResult> Update(
        string key, UpdateSettingRequest req, ISettingService settings, CancellationToken ct)
    {
        var definition = SettingDefinitions.Find(key);
        if (definition is null || !definition.Scopes.Contains(SettingScope.App))
            throw new NotFoundException("Setting", key);

        if (!MatchesKind(req.Value, definition.Type))
            throw new BadRequestException($"The value doesn't match the type of setting '{key}'.");

        await settings.SetAsync(key, req.Value, SettingScope.App, cancellationToken: ct);
        return Results.NoContent();
    }

    private static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone(); // detach so it survives the document's disposal
    }

    private static string KindOf(Type type) =>
        type == typeof(bool) ? "boolean"
        : type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(decimal) ? "number"
        : "string";

    private static bool MatchesKind(JsonElement value, Type type) => KindOf(type) switch
    {
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "number" => value.ValueKind == JsonValueKind.Number,
        _ => value.ValueKind == JsonValueKind.String,
    };
}

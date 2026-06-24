using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Caching;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Platform.Settings;

public interface ISettingService
{
    /// <summary>Resolves a value walking User → Tenant → App → registered default.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key, T value, SettingScope scope, string? scopeId = null,
        CancellationToken cancellationToken = default);
}

public sealed class SettingService(
    AppDbContext db, ICache cache, ITenantContext tenant, IHttpContextAccessor httpContextAccessor)
    : ISettingService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Most specific scope wins.
        var candidates = new List<(SettingScope Scope, string? ScopeId)>();
        if (userId is not null) candidates.Add((SettingScope.User, userId));
        candidates.Add((SettingScope.Tenant, tenant.TenantId));
        candidates.Add((SettingScope.App, null));

        foreach (var (scope, scopeId) in candidates)
        {
            var json = await cache.GetOrSetAsync(
                CacheKey(key, scope, scopeId),
                async ct => (await db.Set<Setting>().AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == key && s.Scope == scope && s.ScopeId == scopeId, ct))?.ValueJson,
                tags: [Tag(key)],
                cancellationToken: cancellationToken);

            if (json is not null) return JsonSerializer.Deserialize<T>(json);
        }

        return SettingDefinitions.Find(key)?.DefaultValue is T fallback ? fallback : default;
    }

    public async Task SetAsync<T>(
        string key, T value, SettingScope scope, string? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        var updatedBy = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var existing = await db.Set<Setting>()
            .FirstOrDefaultAsync(s => s.Key == key && s.Scope == scope && s.ScopeId == scopeId, cancellationToken);

        if (existing is null)
        {
            db.Add(new Setting
            {
                Key = key, Scope = scope, ScopeId = scopeId, ValueJson = json,
                UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = updatedBy,
            });
        }
        else
        {
            existing.ValueJson = json;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = updatedBy;
        }

        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateTagAsync(Tag(key), cancellationToken);
    }

    private static string CacheKey(string key, SettingScope scope, string? scopeId) =>
        $"setting:{key}:{scope}:{scopeId ?? "-"}";

    private static string Tag(string key) => $"setting:{key}";
}

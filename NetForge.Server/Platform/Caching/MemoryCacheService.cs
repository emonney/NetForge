using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace NetForge.Server.Platform.Caching;

/// <summary>
/// In-memory <see cref="ICache"/>. Tag → key membership is tracked so InvalidateTag can
/// evict every entry written under a tag. Singleton (the tag map is shared).
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache) : ICache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    // tag → set of keys. Inner dictionary used as a concurrent set.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagKeys = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(cache.TryGetValue(key, out var value) && value is T typed ? typed : default);

    public Task SetAsync<T>(
        string key, T value, TimeSpan? ttl = null,
        IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        cache.Set(key, value, ttl ?? DefaultTtl);
        if (tags is not null)
            foreach (var tag in tags)
                _tagKeys.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>()).TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null,
        IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is T typed) return typed;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, tags, cancellationToken);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task InvalidateTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_tagKeys.TryRemove(tag, out var keys))
            foreach (var key in keys.Keys)
                cache.Remove(key);
        return Task.CompletedTask;
    }
}

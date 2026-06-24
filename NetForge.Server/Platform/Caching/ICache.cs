namespace NetForge.Server.Platform.Caching;

/// <summary>
/// Cache abstraction over IMemoryCache (default) / IDistributedCache (Redis swap).
/// Supports tagged invalidation: set values with tags, drop them all by tag.
/// </summary>
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key, T value, TimeSpan? ttl = null,
        IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

    Task<T> GetOrSetAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null,
        IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task InvalidateTagAsync(string tag, CancellationToken cancellationToken = default);
}

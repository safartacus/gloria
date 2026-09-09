namespace Gloria.Application.Abstractions;

/// <summary>
/// Distributed cache (Redis). Invalidation uses <b>generation tokens</b>: cache keys embed a
/// generation number read from <see cref="GetGenerationAsync"/>; bumping it via
/// <see cref="BumpGenerationAsync"/> orphans every key of that generation (they expire by TTL).
/// This avoids wildcard deletes.
/// </summary>
public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Current generation for a namespace (1 if never bumped).</summary>
    Task<long> GetGenerationAsync(string generationKey, CancellationToken ct = default);

    /// <summary>Atomically increments the generation for a namespace.</summary>
    Task BumpGenerationAsync(string generationKey, CancellationToken ct = default);
}

public static class CacheStoreExtensions
{
    /// <summary>Cache-aside: return the cached value or run <paramref name="factory"/>, cache it, return it.</summary>
    public static async Task<T> GetOrCreateAsync<T>(
        this ICacheStore store, string key, TimeSpan ttl, Func<Task<T>> factory, CancellationToken ct = default)
    {
        var cached = await store.GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory();
        if (value is not null) await store.SetAsync(key, value, ttl, ct);
        return value;
    }
}

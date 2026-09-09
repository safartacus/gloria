using System.Text.Json;
using Gloria.Application.Abstractions;
using StackExchange.Redis;

namespace Gloria.Infrastructure.Caching;

public sealed class RedisCacheStore : ICacheStore
{
    private const string Prefix = "gloria:v1:";
    private readonly IDatabase _db;

    public RedisCacheStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Prefix + key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, CacheJson.Options);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) =>
        _db.StringSetAsync(Prefix + key, JsonSerializer.Serialize(value, CacheJson.Options), ttl);

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _db.KeyDeleteAsync(Prefix + key);

    public async Task<long> GetGenerationAsync(string generationKey, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Prefix + generationKey);
        return value.TryParse(out long gen) ? gen : 0; // unset = generation 0; first bump -> 1
    }

    public Task BumpGenerationAsync(string generationKey, CancellationToken ct = default) =>
        _db.StringIncrementAsync(Prefix + generationKey);
}

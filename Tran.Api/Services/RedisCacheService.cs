using System.Text.Json;
using StackExchange.Redis;

namespace Tran.Api.Services;

/// <summary>
/// Redis 캐시 서비스 구현
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    private readonly string _instanceName;
    private readonly bool _isEnabled;

    public RedisCacheService(IConfiguration configuration, IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
        _instanceName = configuration.GetValue<string>("Redis:InstanceName") ?? "TranERP_";
        _isEnabled = _redis != null;

        if (_isEnabled)
        {
            _db = _redis!.GetDatabase();
        }
    }

    private string GetKey(string key) => $"{_instanceName}{key}";

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!_isEnabled || _db == null) return default;

        try
        {
            var value = await _db.StringGetAsync(GetKey(key));
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (!_isEnabled || _db == null) return;

        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(GetKey(key), serialized, expiration ?? TimeSpan.FromMinutes(30));
        }
        catch
        {
            // Silently fail - cache is optional
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (!_isEnabled || _db == null) return;

        try
        {
            await _db.KeyDeleteAsync(GetKey(key));
        }
        catch
        {
            // Silently fail
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!_isEnabled || _redis == null) return;

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: GetKey(pattern)).ToArray();
            if (keys.Length > 0)
            {
                await _db!.KeyDeleteAsync(keys);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (!_isEnabled || _db == null) return false;

        try
        {
            return await _db.KeyExistsAsync(GetKey(key));
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Redis 미사용 시 메모리 캐시 폴백 구현
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, (object Value, DateTime Expiry)> _cache = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
        {
            return Task.FromResult((T?)entry.Value);
        }
        _cache.Remove(key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        _cache[key] = (value!, DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(30)));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern.Replace("*", ""))).ToList();
        foreach (var key in keysToRemove) _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_cache.ContainsKey(key) && _cache[key].Expiry > DateTime.UtcNow);
    }
}

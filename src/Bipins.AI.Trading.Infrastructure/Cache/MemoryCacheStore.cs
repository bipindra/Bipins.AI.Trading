using Bipins.AI.Trading.Application.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace Bipins.AI.Trading.Infrastructure.Cache;

public class MemoryCacheStore : ICacheStore
{
    private readonly IMemoryCache _cache;
    
    public MemoryCacheStore(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _cache.TryGetValue(key, out object? value);
        if (value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }
        if (value is string strValue && typeof(T) == typeof(string))
        {
            return Task.FromResult((T)(object)strValue);
        }
        return Task.FromResult<T?>(null);
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }
        
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
    
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }
}

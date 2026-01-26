using Bipins.AI.Trading.Infrastructure.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Bipins.AI.Trading.Tests.Unit;

public class CacheStoreTests
{
    private readonly MemoryCacheStore _cacheStore;
    private readonly IMemoryCache _memoryCache;
    
    public CacheStoreTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheStore = new MemoryCacheStore(_memoryCache);
    }
    
    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyNotFound()
    {
        // Act
        var result = await _cacheStore.GetAsync<string>("nonexistent");
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task SetAsync_And_GetAsync_ShouldWork()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        
        // Act
        await _cacheStore.SetAsync(key, value);
        var result = await _cacheStore.GetAsync<string>(key);
        
        // Assert
        result.Should().Be(value);
    }
    
    [Fact]
    public async Task RemoveAsync_ShouldRemoveKey()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheStore.SetAsync(key, value);
        
        // Act
        await _cacheStore.RemoveAsync(key);
        var result = await _cacheStore.GetAsync<string>(key);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheStore.SetAsync(key, value);
        
        // Act
        var exists = await _cacheStore.ExistsAsync(key);
        
        // Assert
        exists.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyNotExists()
    {
        // Act
        var exists = await _cacheStore.ExistsAsync("nonexistent");
        
        // Assert
        exists.Should().BeFalse();
    }
    
    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpire()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        
        // Act
        await _cacheStore.SetAsync(key, value, TimeSpan.FromMilliseconds(100));
        await Task.Delay(150);
        var result = await _cacheStore.GetAsync<string>(key);
        
        // Assert
        result.Should().BeNull();
    }
}

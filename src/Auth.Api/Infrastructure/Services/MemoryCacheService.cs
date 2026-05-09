using Auth.Api.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Services;

/// <summary>
/// Memory-based cache implementation.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection")]
internal sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache.</param>
    public MemoryCacheService(IMemoryCache memoryCache) {
        this.memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public T Retrieve<T>(string key) {
        return this.memoryCache.Get<T>(key)!;
    }

    /// <inheritdoc />
    public void Store<T>(string key, T value, TimeSpan expiration) {
        this.memoryCache.Set(key, value, expiration);
    }

    /// <inheritdoc />
    public void Remove(string key) {
        this.memoryCache.Remove(key);
    }

    public bool TryRetrieve<T>(string key, out T value) {
        return this.memoryCache.TryGetValue<T>(key, out value!);
    }
}

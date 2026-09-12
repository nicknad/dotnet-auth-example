namespace Auth.Api.Abstractions;

/// <summary>
/// Interface for key-value storage.
/// Makes it possible to abstract away the underlying caching mechanism (e.g., in-memory, distributed cache) 
/// and allows for easier testing and flexibility in implementation.
/// </summary>
internal interface ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value or default.</returns>
    T Retrieve<T>(string key);

    /// <summary>
    /// Try Retrieves a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value.</param>
    /// <returns>true if the value was retrieved; otherwise, false.</returns>
    bool TryRetrieve<T>(string key, out T value);

    /// <summary>
    /// Stores a value in the cache with a specific expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The expiration time span.</param>
    void Store<T>(string key, T value, TimeSpan expiration);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    void Remove(string key);
}

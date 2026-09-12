namespace Auth.Api.Common;

/// <summary>
/// Central definitions of cache keys used across the application.
/// </summary>
internal static class CacheKeys
{
    /// <summary>
    /// Builds the cache key holding a user's token version and active flag.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The cache key.</returns>
    public static string UserValidation(string userId) => $"user:{userId}:validation";
}

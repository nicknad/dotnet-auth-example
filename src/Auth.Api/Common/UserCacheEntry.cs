namespace Auth.Api.Common;

/// <summary>
/// Cache entry for user validation state.
/// Should use the UserId as cache key.
/// </summary>
/// <param name="TokenVersion">The user's current token version</param>
/// <param name="IsActive">Whether the user account is active</param>
internal record struct UserCacheEntry(int TokenVersion, bool IsActive);

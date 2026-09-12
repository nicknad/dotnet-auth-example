using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Auth.Api.Middleware;

/// <summary>
/// Middleware to validate JWT tokens on each request.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection")]
internal sealed class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next) {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserStorage userManager, ICacheService cache, ILogger<TokenValidationMiddleware> logger) {
        // If the user is not authenticated, it means endpoint does not require authentication, so we can skip validation
        if (!context.User.Identity?.IsAuthenticated ?? true) {
            await this._next(context).ConfigureAwait(false);
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenVersionClaim = context.User.FindFirst("ver")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenVersionClaim)) {
            logger.LogWarning("Token validation failed: missing userId or tokenVersion claims");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var cacheKey = CacheKeys.UserValidation(userId);
        if (!cache.TryRetrieve<UserCacheEntry>(cacheKey, out UserCacheEntry cacheEntry)) {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) {
                logger.LogWarning("Token validation failed: user {UserId} not found", userId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            cacheEntry = new (user.TokenVersion, user.IsActive);
            cache.Store<UserCacheEntry>(cacheKey, cacheEntry, TimeSpan.FromMinutes(5));
        }

        if (!cacheEntry.IsActive) {
            logger.LogWarning("Token validation failed: user {UserId} is inactive", userId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (cacheEntry.TokenVersion.ToString() != tokenVersionClaim) {
            logger.LogWarning("Token validation failed: token version mismatch for user {UserId}", userId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await this._next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension methods for TokenValidationMiddleware.
/// </summary>
internal static class TokenValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds the token validation middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder app) {
        return app.UseMiddleware<TokenValidationMiddleware>();
    }
}

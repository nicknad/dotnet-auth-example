using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.Token;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Infrastructure.Services;

internal sealed class TokenHandler(IOptions<JwtOptions> jwtOptions, IUserStorage userStorage, IAuthLogger logger, TimeProvider timeProvider, ICacheService cache) : ITokenHandler
{
    private JwtOptions Jwt => jwtOptions.Value;

    public async Task<TokenResult> ValidateLoginAndCreateTokenAsync(string email, string password) {
        logger.LogInformation("Login attempt for email {Email}", MaskEmail(email));

        var user = await userStorage.FindByEmailAsync(email);

        if (user == null) {
            logger.LogWarning("Login failed: user not found for email {Email}", MaskEmail(email));
            return new TokenResult(TokenResultStatus.UserNotFound);
        }

        if (!user.IsActive) {
            logger.LogWarning("Login failed: user account is disabled for email {Email}", MaskEmail(email));
            return new TokenResult(TokenResultStatus.UserNotActive);
        }

        var isValidPassword = await userStorage.CheckPasswordAsync(user, password);

        if (!isValidPassword) {
            logger.LogWarning("Login failed: invalid password for {UserID}", user.Id);
            // escalate to monitoring if there are multiple failed attempts for the same email or IP address,
            // consider implementing account lockout after a certain number of failed attempts.
            return new TokenResult(TokenResultStatus.InvalidCredentials);
        }

        (string? accessToken, DateTime expiresAt) = await GenerateJwtToken(user);
        string refreshToken = GenerateRefreshToken();
        DateTime refreshTokenExpiry = timeProvider.GetUtcNow().AddDays(7).UtcDateTime;

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = refreshTokenExpiry;
        await userStorage.UpdateAsync(user);

        logger.LogInformation("Login successful for user {UserId}", user.Id);

        return new TokenResult(TokenResultStatus.Success, new TokenDTO(accessToken, refreshToken, expiresAt));
    }

    /// <summary>
    /// Maske Pii data for the auth log. 
    /// </summary>
    /// <param name="email">The email to mask</param>
    /// <returns>Masked email</returns>
    /// #TODO: Consider using a more robust PII masking library, consider hashing the email to make it tracable.
    private static string MaskEmail(string email) {
        if (string.IsNullOrEmpty(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase)) {
            return "[invalid-email]";
        }

        var parts = email.Split('@');
        if (parts[0].Length <= 2) {
            return $"***{parts[1]}";
        }

        return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
    }

    internal async Task<(string Token, DateTime ExpiresAt)> GenerateJwtToken(ApplicationUser user) {
        var keyString = Jwt.Key;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var userRoles = await userStorage.GetRolesByUserAsync(user);

        var claims = new List<Claim>
        {
          new(JwtRegisteredClaimNames.Sub, user.Id),
          new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
          new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
          new(ClaimTypes.NameIdentifier, user.Id),
          new Claim("ver", user.TokenVersion.ToString()),
          new Claim("active", user.IsActive.ToString()),
        };

        foreach (var role in userRoles) {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = timeProvider.GetUtcNow().AddMinutes(15).UtcDateTime;

        var token = new JwtSecurityToken(
            issuer: Jwt.Issuer,
            audience: Jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

#pragma warning disable CA1031 // Do not catch general exception types
    public async Task<AuthApiResult> LogoutAsync(string userId, bool revokeAllTokens) {
        try {
            var user = await userStorage.FindByIdAsync(userId);
            if (user == null) {
                logger.LogWarning("Logout failed: user not found for {UserId}", userId);
                return AuthApiResult.Failed("User not found");
            }

            if (revokeAllTokens) {
                user.TokenVersion++;
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                var updateResult = await userStorage.UpdateAsync(user);

                if (!updateResult.Succeeded) {
                    logger.LogWarning("Logout failed: could not update user {UserId}", userId);
                    return updateResult;
                }

                logger.LogInformation("All tokens revoked for user {UserId}", userId);
            } else {
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                var updateResult = await userStorage.UpdateAsync(user);

                if (!updateResult.Succeeded) {
                    logger.LogWarning("Logout failed: could not update user {UserId}", userId);
                    return updateResult;
                }

                logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            }

            // Invalidate cache to prevent access with old tokens
            var cacheKey = $"user:{userId}:validation";
            cache.Remove(cacheKey);

            return AuthApiResult.Success;
        } catch (Exception ex) {
            logger.LogWarning("Logout error for user {UserId}: {Error}", userId, ex.Message);
            return AuthApiResult.Failed("An error occurred during logout");
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types

#pragma warning disable CA1822 // Mark members as static
    private string GenerateRefreshToken() {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
#pragma warning restore CA1822 // Mark members as static

    public async Task<TokenResult> RefreshTokenAsync(string refreshToken) {
#pragma warning disable CA1031 // Do not catch general exception types
        try {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshToken)) {
                logger.LogWarning("Refresh token failed: token is null or empty");
                return new TokenResult(TokenResultStatus.InvalidCredentials);
            }

            // Find user by refresh token
            var user = await userStorage.FindByRefreshTokenAsync(refreshToken);

            if (user == null) {
                logger.LogWarning("Refresh token failed: token not found");
                return new TokenResult(TokenResultStatus.InvalidCredentials);
            }

            // Validate token expiry
            var currentTime = timeProvider.GetUtcNow().UtcDateTime;
            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < currentTime) {
                logger.LogWarning("Refresh token failed: token expired or null for user {UserId}", user.Id);
                return new TokenResult(TokenResultStatus.InvalidCredentials);
            }

            // Validate user is active
            if (!user.IsActive) {
                logger.LogWarning("Refresh token failed: user inactive for {UserId}", user.Id);
                return new TokenResult(TokenResultStatus.UserNotActive);
            }

            // Validate user is not deleted
            if (user.IsDeleted) {
                logger.LogWarning("Refresh token failed: user deleted for {UserId}", user.Id);
                return new TokenResult(TokenResultStatus.UserDeleted);
            }

            // Generate new access token
            var (accessToken, expiresAt) = await GenerateJwtToken(user);

            // Generate new refresh token
            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = timeProvider.GetUtcNow().AddDays(7).UtcDateTime;

            // Update user with new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAt = refreshTokenExpiry;
            var updateResult = await userStorage.UpdateAsync(user);

            if (!updateResult.Succeeded) {
                logger.LogWarning("Refresh token failed: could not update user {UserId}", user.Id);
                return new TokenResult(TokenResultStatus.UnknownError);
            }

            // Invalidate cache to ensure fresh validation data is loaded on next request
            var cacheKey = $"user:{user.Id}:validation";
            cache.Remove(cacheKey);

            logger.LogInformation("Refresh token successful for user {UserId}", user.Id);

            return new TokenResult(TokenResultStatus.Success, new TokenDTO(accessToken, newRefreshToken, expiresAt));
        } catch (Exception ex) {
            logger.LogWarning("Refresh token error: {Error}", ex.Message);
            return new TokenResult(TokenResultStatus.UnknownError);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}

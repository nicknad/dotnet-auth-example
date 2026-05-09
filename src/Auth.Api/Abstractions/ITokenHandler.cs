using Auth.Api.Common;
using Auth.Api.Common.Token;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Abstractions;

/// <summary>
/// Provides methods for creating, validating, and invalidating authentication tokens.
/// </summary>
internal interface ITokenHandler
{
    /// <summary>
    /// Validates the provided email and password, and if valid, creates a new authentication token.
    /// </summary>
    /// <returns>A Result object with the token and its expiration date/time.</returns>
    Task<TokenResult> ValidateLoginAndCreateTokenAsync(string email, string password);

    /// <summary>
    /// Logs out a user by clearing refresh tokens and optionally revoking all tokens.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="revokeAllTokens">If true, revokes all tokens and disables the user; otherwise, only clears the refresh token.</param>
    /// <returns>An AuthApiResult indicating success or failure with any associated errors.</returns>
    Task<AuthApiResult> LogoutAsync(string userId, bool revokeAllTokens);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate and use for generating a new access token.</param>
    /// <returns>A TokenResult with new access token and refresh token, or failure status with error details.</returns>
    Task<TokenResult> RefreshTokenAsync(string refreshToken);
}

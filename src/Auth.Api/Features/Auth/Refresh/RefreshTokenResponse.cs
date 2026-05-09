namespace Auth.Api.Features.Auth.Refresh;

/// <summary>
/// Represents a response after a successful token refresh.
/// </summary>
internal sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

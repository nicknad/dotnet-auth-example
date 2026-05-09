namespace Auth.Api.Features.Auth.Login;

/// <summary>
/// Represents a response after a successful login.
/// </summary>
internal sealed record LoginResponse(string AccessToken, string RefreshToken, string Email, DateTime ExpiresAt);

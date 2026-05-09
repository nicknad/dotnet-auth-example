namespace Auth.Api.Features.Auth.Logout;

/// <summary>
/// Request model for logout endpoint.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal
public sealed record LogoutRequest(bool RevokeAllTokens = false);
#pragma warning restore CA1515 // Consider making public types internal

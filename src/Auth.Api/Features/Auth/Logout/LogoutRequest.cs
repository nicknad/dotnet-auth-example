namespace Auth.Api.Features.Auth.Logout;

/// <summary>
/// Request model for logout endpoint. Accepted for wire compatibility but ignored: logout always revokes all tokens.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal
public sealed record LogoutRequest(bool RevokeAllTokens = true);
#pragma warning restore CA1515 // Consider making public types internal

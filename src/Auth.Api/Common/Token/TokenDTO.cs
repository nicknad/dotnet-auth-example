namespace Auth.Api.Common.Token;

internal record TokenDTO(string Token, string RefreshToken, DateTime ExpiresAt);


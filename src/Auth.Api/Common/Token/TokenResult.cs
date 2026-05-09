namespace Auth.Api.Common.Token;

internal record struct TokenResult(TokenResultStatus Status, TokenDTO? Token = null);

internal enum TokenResultStatus
{
    Success,
    InvalidCredentials,
    UserNotFound,
    UserNotActive,
    UserDeleted,
    UnknownError
}

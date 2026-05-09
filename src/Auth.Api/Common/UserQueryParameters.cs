namespace Auth.Api.Common;

internal record struct UserQueryParameters(
    int? PageSize = 10,
    int? Page = 0,
    string? Name = null,
    string? Email = null,
    string? Role = null
);

namespace Auth.Api.Common.Constants;

internal static class JWT
{
    public const int AccessTokenExpiresInMinutes = 15;
    public const int RefreshTokenExpiresInDays = 7;
}

internal static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

internal static class RateLimiting
{
    public const int PermitLimit = 50;
    public const int Window = 1;
}

internal static class Pagination
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
}



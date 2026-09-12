using Auth.Api.Common.Constants;

namespace Auth.Api.Common;

internal record struct UserQueryParameters(
    int? PageSize = Pagination.DefaultPageSize,
    int? Page = 0,
    string? Name = null,
    string? Email = null,
    string? Role = null
)
{
    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 256;
    private const int MaxRoleLength = 50;

    /// <summary>
    /// Validates the query parameters, returning the first error message or null when valid.
    /// </summary>
    /// <returns>An error message or null.</returns>
    public string? Validate() {
        if (Page is < 0) {
            return "Page must be greater than or equal to 0.";
        }

        if (PageSize is < 1 or > Pagination.MaxPageSize) {
            return $"PageSize must be between 1 and {Pagination.MaxPageSize}.";
        }

        if (Name is { Length: > MaxNameLength }) {
            return $"Name filter must not exceed {MaxNameLength} characters.";
        }

        if (Email is { Length: > MaxEmailLength }) {
            return $"Email filter must not exceed {MaxEmailLength} characters.";
        }

        if (Role is { Length: > MaxRoleLength }) {
            return $"Role filter must not exceed {MaxRoleLength} characters.";
        }

        return null;
    }
}

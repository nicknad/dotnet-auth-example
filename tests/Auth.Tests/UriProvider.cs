using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Tests;

internal static class UriProvider
{
    internal static Uri AuthUrl => new Uri("/api/v1/auth/login", UriKind.Relative);
    internal static Uri RefreshUrl => new Uri("/api/v1/auth/refresh", UriKind.Relative);
    internal static Uri LogoutUrl => new Uri("/api/v1/auth/logout", UriKind.Relative);
    internal static Uri RegisterUrl => new Uri("/api/v1/users/register", UriKind.Relative);
    internal static Uri ListUsersUrl => new Uri("/api/v1/users", UriKind.Relative);
    /// <summary>
    /// Returns the base URL with added query parameters
    /// </summary>
    internal static Uri GetListUsersWithQueryParams(Dictionary<string, string?> queryParams, int? page = null, int? pageSize = null) {
        if (page.HasValue)
            queryParams.Add("page", page.Value.ToString());

        if (pageSize.HasValue)
            queryParams.Add("pageSize", pageSize.Value.ToString());

        if (queryParams.Count > 0) {
            return new Uri(QueryHelpers.AddQueryString(ListUsersUrl.OriginalString, queryParams), UriKind.Relative);
        }

        return ListUsersUrl;
    }

    internal static Uri GetUserUrl(string userId) => new Uri($"/api/v1/users/{userId}", UriKind.Relative);
}

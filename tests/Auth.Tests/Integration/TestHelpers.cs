using Auth.Api.Features.Auth.Login;
using Auth.Api.Features.Auth.Logout;
using Auth.Api.Features.Auth.Refresh;
using Auth.Api.Features.Users.GetById;
using Auth.Api.Features.Users.Patch;
using Auth.Api.Features.Users.Register;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Auth.Tests.Integration;

/// <summary>
/// Helper methods for integration tests to improve readability and reduce duplication.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Registers a new test user with the provided credentials.
    /// </summary>
    public static async Task<string> RegisterTestUserAsync(
        HttpClient client,
        string email,
        string firstName,
        string lastName,
        string password = "Password123!")
    {
        var request = new RegisterUserRequest(email, password, password, firstName, lastName);
        var response = await client.PostAsJsonAsync(
            UriProvider.RegisterUrl,
            request,
            CancellationToken.None);

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>(CancellationToken.None);
        return result?.Id ?? throw new InvalidOperationException($"Registration failed for {email}");
    }

    /// <summary>
    /// Registers a new test user and returns both the user ID and login token.
    /// </summary>
    public static async Task<(string UserId, string AccessToken)> RegisterAndLoginAsync(
        HttpClient client,
        string email,
        string firstName,
        string lastName,
        string password = "Password123!")
    {
        var userId = await RegisterTestUserAsync(client, email, firstName, lastName, password);
        var token = await LoginAndGetTokenAsync(client, email, password);
        return (userId, token);
    }

    /// <summary>
    /// Logs in a user and returns their access token.
    /// </summary>
    public static async Task<string> LoginAndGetTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        var request = new LoginRequest(email, password);
        var response = await client.PostAsJsonAsync(
            UriProvider.AuthUrl,
            request,
            CancellationToken.None);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(CancellationToken.None);
        return result?.AccessToken ?? throw new InvalidOperationException($"Login failed for {email}");
    }

    /// <summary>
    /// Gets the admin user's access token.
    /// </summary>
    public static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        return await LoginAndGetTokenAsync(client, "admin@example.com", "Admin123!");
    }

    /// <summary>
    /// Sends a login request and returns the full response.
    /// </summary>
    public static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var request = new LoginRequest(email, password);
        return await client.PostAsJsonAsync(
            UriProvider.AuthUrl,
            request,
            CancellationToken.None);
    }

    /// <summary>
    /// Sends a registration request and returns the full response.
    /// </summary>
    public static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string password,
        string confirmPassword,
        string firstName,
        string lastName,
        IReadOnlyList<string>? roles = null,
        string? accessToken = null)
    {
        var request = new RegisterUserRequest(email, password, confirmPassword, firstName, lastName, roles);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, UriProvider.RegisterUrl);
        httpRequest.Content = JsonContent.Create(request);
        if (!string.IsNullOrEmpty(accessToken)) {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        var response = await client.SendAsync(httpRequest, CancellationToken.None);
        return response;
    }

    /// <summary>
    /// Creates an authenticated GET request with Bearer token.
    /// </summary>
    public static HttpRequestMessage CreateAuthenticatedGetRequest(Uri url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>
    /// Creates an authenticated PATCH request with Bearer token and JSON content.
    /// </summary>
    public static HttpRequestMessage CreateAuthenticatedPatchRequest(
        Uri url,
        string accessToken,
        PatchUserRequest patchRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Content = JsonContent.Create(patchRequest);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>
    /// Creates an authenticated DELETE request with Bearer token.
    /// </summary>
    public static HttpRequestMessage CreateAuthenticatedDeleteRequest(Uri url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>
    /// Registers a user, logs in, and performs a GET request to retrieve user data.
    /// </summary>
    public static async Task<GetUserResponse?> RegisterLoginAndGetUserAsync(
        HttpClient client,
        string email,
        string firstName,
        string lastName)
    {
        var (userId, accessToken) = await RegisterAndLoginAsync(client, email, firstName, lastName);
        using var request = CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response = await client.SendAsync(request, CancellationToken.None);
        return await response.Content.ReadFromJsonAsync<GetUserResponse>(CancellationToken.None);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    public static async Task<HttpResponseMessage> RefreshTokenAsync(
        HttpClient client,
        string refreshToken)
    {
        var request = new RefreshTokenRequest(refreshToken);
        return await client.PostAsJsonAsync(
            UriProvider.RefreshUrl,
            request,
            CancellationToken.None);
    }

    /// <summary>
    /// Gets a refresh token from a login response.
    /// </summary>
    public static async Task<string> GetRefreshTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await LoginAsync(client, email, password);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(CancellationToken.None);
        return result?.RefreshToken ?? throw new InvalidOperationException($"Failed to get refresh token for {email}");
    }

    /// <summary>
    /// Logs out a user with optional token revocation.
    /// </summary>
    public static async Task<HttpResponseMessage> LogoutAsync(
        HttpClient client,
        string accessToken,
        bool revokeAllTokens = false)
    {
        var request = new LogoutRequest(revokeAllTokens);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, UriProvider.LogoutUrl);
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(httpRequest, CancellationToken.None);
        httpRequest.Dispose();
        return response;
    }
}

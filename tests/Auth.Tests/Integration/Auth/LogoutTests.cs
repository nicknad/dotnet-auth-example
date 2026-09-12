using System.Net;

namespace Auth.Tests.Integration.Auth;

public sealed class LogoutTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public LogoutTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task LogoutWithValidAccessTokenReturnsOk() {
        var email = "logouttest@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Logout", "Test");

        var response = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWithoutBodyReturnsOk() {
        var email = "logoutnobody@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Logout", "NoBody");

        using var request = new HttpRequestMessage(HttpMethod.Post, UriProvider.LogoutUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWithoutAccessTokenReturnsUnauthorized() {
        var invalidToken = "invalid.access.token";
        var response = await TestHelpers.LogoutAsync(_client, invalidToken, revokeAllTokens: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWithEmptyAccessTokenReturnsUnauthorized() {
        var response = await TestHelpers.LogoutAsync(_client, "", revokeAllTokens: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutWithRevokeAllTokensInvalidatesAccessAndRefreshTokens() {
        var email = "revokealltest@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Revoke", "All");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        var response = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), accessToken);
        var getResponse = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var refreshResponse = await TestHelpers.RefreshTokenAsync(_client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutWithoutRevokeAllTokensReturnsOk() {
        var email = "logoutnorevoke@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Logout", "NoRevoke");

        var response = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LogoutClearsRefreshToken() {
        var email = "cleartokentest@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Clear", "Token");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        // First logout should succeed
        var logoutResponse = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // The refresh token must be unusable after logout
        var refreshResponse = await TestHelpers.RefreshTokenAsync(_client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        // The current access token should still work (until expiration)
        using var request = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), accessToken);
        var getResponse = await _client.SendAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutMultipleTimesWithSameTokenSucceeds() {
        var email = "multilogout@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Multi", "Logout");

        // First logout should succeed
        var firstLogout = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        Assert.Equal(HttpStatusCode.OK, firstLogout.StatusCode);

        // Logout is idempotent while the access token is still valid
        var secondLogout = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        Assert.Equal(HttpStatusCode.OK, secondLogout.StatusCode);
    }
}

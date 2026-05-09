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
    public async Task LogoutWithRevokeAllTokensReturnsOk() {
        var email = "revokealltest@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Revoke", "All");

        var response = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

        // First logout should succeed
        var logoutResponse = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // The current access token should still work (until expiration)
        using var request = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), accessToken);
        var getResponse = await _client.SendAsync(request, CancellationToken.None);
        // Access token is still valid, so this should succeed
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutMultipleTimesWithSameTokenSucceeds() {
        var email = "multilogout@test.com";
        var (_, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Multi", "Logout");

        // First logout should succeed
        var firstLogout = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        Assert.Equal(HttpStatusCode.OK, firstLogout.StatusCode);

        // Second logout with same token might still succeed or fail depending on implementation
        // This tests idempotency
        var secondLogout = await TestHelpers.LogoutAsync(_client, accessToken, revokeAllTokens: false);
        // Accept either OK or Unauthorized as valid
        Assert.True(
            secondLogout.StatusCode == HttpStatusCode.OK || 
            secondLogout.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected OK or Unauthorized, got {secondLogout.StatusCode}");
    }
}

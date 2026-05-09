using Auth.Api.Features.Auth.Refresh;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Auth;

public sealed class RefreshTokenTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public RefreshTokenTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task RefreshTokenWithValidTokenReturnsNewAccessToken() {
        var email = "refreshtest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Refresh", "Test");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        var response = await TestHelpers.RefreshTokenAsync(_client, refreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task RefreshTokenWithInvalidTokenReturnsUnauthorized() {
        var invalidToken = "invalid.refresh.token";
        var response = await TestHelpers.RefreshTokenAsync(_client, invalidToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenWithEmptyTokenReturnsBadRequest() {
        var response = await TestHelpers.RefreshTokenAsync(_client, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenWithWhitespaceTokenReturnsBadRequest() {
        var response = await TestHelpers.RefreshTokenAsync(_client, "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenWithTooLongTokenReturnsBadRequest() {
        var tooLongToken = new string('a', 501);
        var response = await TestHelpers.RefreshTokenAsync(_client, tooLongToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenReturnsNewRefreshToken() {
        var email = "refreshnewtest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Refresh", "New");
        var initialRefreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        var response = await TestHelpers.RefreshTokenAsync(_client, initialRefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenWithExpiredTokenReturnsUnauthorized() {
        var email = "expiredtest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Expired", "Test");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        // First refresh should succeed
        var firstRefresh = await TestHelpers.RefreshTokenAsync(_client, refreshToken);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        // Try to use the old refresh token again should fail
        var secondRefresh = await TestHelpers.RefreshTokenAsync(_client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }
}

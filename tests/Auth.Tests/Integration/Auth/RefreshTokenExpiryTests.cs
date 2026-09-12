using System.Net;

namespace Auth.Tests.Integration.Auth;

public sealed class RefreshTokenExpiryTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public RefreshTokenExpiryTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task RefreshTokenAfterExpiryReturnsUnauthorized() {
        var email = "refreshexpiry@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Refresh", "Expiry");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        _fixture.TimeProvider.Advance(TimeSpan.FromDays(8));

        var response = await TestHelpers.RefreshTokenAsync(_client, refreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

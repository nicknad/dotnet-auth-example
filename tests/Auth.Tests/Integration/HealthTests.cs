using Auth.Api.Features.Health;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration;

public sealed class HealthTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public HealthTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task HealthReturnsHealthyStatus() {
        var response = await _client.GetAsync(UriProvider.HealthUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<HealthStatus>(TestContext.Current.CancellationToken);
        Assert.NotNull(status);
        Assert.Equal("healthy", status.Status);
    }
}

using System.Net;

namespace Auth.Tests.Security;

public sealed class SecurityHeadersTests : KestrelTestBase
{
    public SecurityHeadersTests(KestrelFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ResponsesIncludeSecurityHeaders() {
        var response = await HttpClient.GetAsync(UriProvider.HealthUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions));
        Assert.Equal("nosniff", contentTypeOptions.Single());
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }
}

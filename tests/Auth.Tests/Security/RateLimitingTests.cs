using Auth.Api.Common.Constants;
using Auth.Api.Features.Auth.Login;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Security;

/// <summary>
/// Integration tests for rate limiting.
/// </summary>
public sealed class RateLimitingTests : KestrelTestBase
{
    public RateLimitingTests(KestrelFixture fixture) : base(fixture) {}

    /// <summary>
    /// Verifies that the limiter rejects requests once the configured permit limit is exceeded.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RateLimitingExceedThresholdReturnsTooManyRequests() {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var permitLimit = RateLimiting.PermitLimit;

        // Act & Assert
        var sawTooManyRequests = false;
        for (int i = 0; i < permitLimit + 5 && !sawTooManyRequests; i++) {
            var response = await this.HttpClient.PostAsJsonAsync(UriProvider.AuthUrl, request, TestContext.Current.CancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                sawTooManyRequests = true;
            }
        }

        Assert.True(sawTooManyRequests, $"Expected the rate limiter to reject a request within {permitLimit + 5} attempts.");
    }
}

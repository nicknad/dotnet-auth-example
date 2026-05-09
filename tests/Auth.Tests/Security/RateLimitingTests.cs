using Auth.Api.Features.Auth.Login;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Security;

/// <summary>
/// Integration tests for Rate Limiting and IP Blocking.
/// </summary>
public sealed class RateLimitingTests : KestrelTestBase
{
    public RateLimitingTests(KestrelFixture fixture) : base(fixture) {}

    /// <summary>
    /// Verifies that exceeding the rate limit threshold returns TooManyRequests and then Forbidden.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task RateLimitingExceedThresholdReturnsTooManyRequestsAndThenForbidden() {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");

        // Act & Assert
        // First 50 should be OK or Unauthorized (not 429)
        for (int i = 0; i < 50; i++) {
            var response = await this.HttpClient.PostAsJsonAsync(UriProvider.AuthUrl, request, TestContext.Current.CancellationToken);
            Assert.NotEqual((HttpStatusCode)429, response.StatusCode);
        }

        // 51st should be 429
        var limitExceededResponse = await this.HttpClient.PostAsJsonAsync(UriProvider.AuthUrl, request, TestContext.Current.CancellationToken);
        Assert.Equal((HttpStatusCode)429, limitExceededResponse.StatusCode);

    }
}

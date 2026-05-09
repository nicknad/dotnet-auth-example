using Auth.Api.Features.Auth.Login;
using Auth.Api.Features.Users.Patch;
using Auth.Api.Features.Users.Register;
using Auth.Tests.Integration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Auth.Tests.Security;

public sealed class PayloadSizeLimitTests : KestrelTestBase
{
    public PayloadSizeLimitTests(KestrelFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LoginPostRejectsPayloadOver1MB()
    {
        // Arrange
        var largePassword = new string('A', 1024 * 1024 + 1); // 1MB + 1 byte
        var loginRequest = new LoginRequest("toolarge@test.com", largePassword);

        // Act
        var response = await this.HttpClient.PostAsJsonAsync(UriProvider.AuthUrl, loginRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge || (int)response.StatusCode == 413, $"Expected 413, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task RegisterPostRejectsPayloadOver1MB()
    {
        // Arrange
        var largePassword = new string('A', 1024 * 1024 + 1); // 1MB + 1 byte
        var registerRequest = new RegisterUserRequest("toolarge@test.com", largePassword, largePassword, "First", "Last");

        // Act
        var response = await this.HttpClient.PostAsJsonAsync(UriProvider.RegisterUrl, registerRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge || (int)response.StatusCode == 413, $"Expected 413, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task PatchEndpointRejectsPayloadOver1MB()
    {
        // Arrange
        var accessToken = await TestHelpers.LoginAndGetTokenAsync(this.HttpClient, "admin@example.com", "Admin123!");
        var largePassword = new string('A', 1024 * 1024 + 1); // 1MB + 1 byte
        var patchRequest = new PatchUserRequest(null, null, largePassword);

        // Act
        using var patchMsg = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl("some-id"), accessToken, patchRequest);
        var response = await this.HttpClient.SendAsync(patchMsg);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge || (int)response.StatusCode == 413, $"Expected 413, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task RejectsTooManyHeaders() {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));

        for (int i = 0; i < 200; i++) // exceed default 100
        {
            request.Headers.TryAddWithoutValidation($"X-Test-{i}", "A");
        }

        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestHeaderFieldsTooLarge, response.StatusCode);
    }
}

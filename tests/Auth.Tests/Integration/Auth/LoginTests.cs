using Auth.Api.Features.Auth.Login;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Auth;

public sealed class LoginTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public LoginTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task LoginWithValidCredentialsReturnsAccessToken() {
        var email = "logintest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Login", "Test");

        var response = await TestHelpers.LoginAsync(_client, email, "Password123!");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task LoginWithInvalidPasswordReturnsUnauthorized() {
        var email = "authtest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Auth", "Test");

        var response = await TestHelpers.LoginAsync(_client, email, "WrongPassword!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithNonexistentUserReturnsUnauthorized() {
        var response = await TestHelpers.LoginAsync(_client, "nonexistent@test.com", "Password123!");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithEmptyEmailReturnsBadRequest() {
        var response = await TestHelpers.LoginAsync(_client, "", "Password123!");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithEmptyPasswordReturnsBadRequest() {
        var response = await TestHelpers.LoginAsync(_client, "test@example.com", "");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithWhitespaceEmailTrimmed() {
        var email = "trimtest@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Trim", "Test");

        var response = await TestHelpers.LoginAsync(_client, "  trimtest@test.com  ", "Password123!");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithWhitespaceOnlyEmailReturnsBadRequest() {
        var response = await TestHelpers.LoginAsync(_client, "   ", "Password123!");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithWhitespaceOnlyPasswordReturnsBadRequest() {
        var response = await TestHelpers.LoginAsync(_client, "test@example.com", "   ");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithTooLongEmailReturnsBadRequest() {
        var tooLongEmail = new string('a', 250) + "@test.com";
        var response = await TestHelpers.LoginAsync(_client, tooLongEmail, "Password123!");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithTooLongPasswordReturnsBadRequest() {
        var tooLongPassword = new string('a', 200);
        var response = await TestHelpers.LoginAsync(_client, "test@example.com", tooLongPassword);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

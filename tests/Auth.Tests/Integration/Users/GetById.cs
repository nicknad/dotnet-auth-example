using Auth.Api.Features.Auth.Login;
using Auth.Api.Features.Users.GetById;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Users;

public sealed class GetById : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public GetById(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task GetByIdWhenAuthenticatedReturnsOwnData() {
        var email = "getbyid@test.com";
        var userId = await TestHelpers.RegisterTestUserAsync(_client, email, "Get", "ById");

        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email, "Password123!");

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<GetUserResponse>(CancellationToken.None);
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task GetByIdWhenOtherUserReturnsForbidden() {
        var email1 = "user1@test.com";
        var email2 = "user2@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email1, "User", "One");
        var userId2 = await TestHelpers.RegisterTestUserAsync(_client, email2, "User", "Two");

        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email1, "Password123!");

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId2), accessToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdWhenAdminCanAccessAnyUser() {
        var userEmail = "adminaccesstest@test.com";
        var userId = await TestHelpers.RegisterTestUserAsync(_client, userEmail, "Admin", "Access");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl(userId), adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdWithInvalidIdReturnsNotFound() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.GetUserUrl("invalid-id"), adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdWhenUnauthenticatedReturnsUnauthorized() {
        var response = await _client.GetAsync(UriProvider.GetUserUrl("some-id"), CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdWithInvalidAuthorizationSchemeReturnsUnauthorized() {
        using var getRequest = new HttpRequestMessage(
              HttpMethod.Get,
              UriProvider.GetUserUrl("invalid-id"));
        getRequest.Headers.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "Password!123");

        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdWithSqlInjectionInIdReturnsNotFound() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetUserUrl("'; DROP TABLE users; --"), 
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Assert that the users table still exists by trying to list users
        using var getListRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, adminToken);
        var responseList = await _client.SendAsync(getListRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, responseList.StatusCode);
        var users = await responseList.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }
}

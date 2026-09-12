using Auth.Api.Features.Users.Register;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Users;

public sealed class DeleteUser : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public DeleteUser(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task DeleteUserSoftDeletesOwnAccount() {
        var email = "delete@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Delete", "User");

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var loginResp2 = await TestHelpers.LoginAsync(_client, email, "Password123!");
        Assert.Equal(HttpStatusCode.Unauthorized, loginResp2.StatusCode);
    }

    [Fact]
    public async Task DeleteUserOtherUserReturnsForbidden() {
        var email1 = "deleteuser1@test.com";
        var email2 = "deleteuser2@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email1, "Delete", "User1");
        var (userId2, _) = await TestHelpers.RegisterAndLoginAsync(_client, email2, "Delete", "User2");

        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email1, "Password123!");

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId2), accessToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsAdminCanDeleteAnyUser() {
        var email = "deletebyadmin@test.com";
        var userId = await TestHelpers.RegisterTestUserAsync(_client, email, "Delete", "ByAdmin");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), adminToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAlreadyDeletedAsAdminReturnsNotFound() {
        var email = "deletebyadmin@test.com";
        var userId = await TestHelpers.RegisterTestUserAsync(_client, email, "Delete", "ByAdmin");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), adminToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var deleteRequest2 = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), adminToken);
        var response2 = await _client.SendAsync(deleteRequest2, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAlreadyDeletedReturnsUnauthorized() {
        var email = "deletedouble@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Delete", "Double");

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var deleteRequest2 = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response2 = await _client.SendAsync(deleteRequest2, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    [Fact]
    public async Task DeleteUserInvalidatesRefreshToken() {
        var email = "deleterefresh@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Delete", "Refresh");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), accessToken);
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var refreshResponse = await TestHelpers.RefreshTokenAsync(_client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUserUnauthenticatedReturnsUnauthorized() {
        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl("some-id"), "");
        var response = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

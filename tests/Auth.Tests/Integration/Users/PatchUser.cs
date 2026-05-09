using Auth.Api.Features.Users.GetById;
using Auth.Api.Features.Users.Patch;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Users;

public sealed class PatchUser : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public PatchUser(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task PatchUserUpdatesOwnData() {
        var email = "patch@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "User");

        var patchRequest = new PatchUserRequest("UpdatedFirst", "UpdatedLast", null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<GetUserResponse>(CancellationToken.None);
        Assert.NotNull(user);
        Assert.Equal("UpdatedFirst", user.FirstName);
        Assert.Equal("UpdatedLast", user.LastName);
    }

    [Fact]
    public async Task PatchUserCanChangePassword() {
        var email = "patchpwd@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Pwd");

        var patchRequest = new PatchUserRequest(null, null, "NewPassword1!");
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResp2 = await TestHelpers.LoginAsync(_client, email, "NewPassword1!");
        Assert.Equal(HttpStatusCode.OK, loginResp2.StatusCode);
    }

    [Fact]
    public async Task PatchUserOtherUserReturnsForbidden() {
        var email1 = "patchuser1@test.com";
        var email2 = "patchuser2@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email1, "Patch", "User1");
        var (userId2, _) = await TestHelpers.RegisterAndLoginAsync(_client, email2, "Patch", "User2");

        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email1, "Password123!");

        var patchRequest = new PatchUserRequest("Hacked", "Hacked", null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId2), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithWeakPasswordReturnsBadRequest() {
        var email = "patchweak@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Weak");

        var patchRequest = new PatchUserRequest(null, null, "weak");
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserUnauthenticatedReturnsUnauthorized() {
        var patchRequest = new PatchUserRequest(null, null, null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl("some-id"), "", patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithAllNullFieldsReturnsBadRequest() {
        var email = "patchnull@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Null");

        var patchRequest = new PatchUserRequest(null, null, null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithEmptyFirstNameReturnsBadRequest() {
        var email = "patchempty1@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Empty");

        var patchRequest = new PatchUserRequest("", null, null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithEmptyLastNameReturnsBadRequest() {
        var email = "patchempty2@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Empty");

        var patchRequest = new PatchUserRequest(null, "", null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithTooLongFirstNameReturnsBadRequest() {
        var email = "patchlong1@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Long");

        var tooLongFirstName = new string('a', 101);
        var patchRequest = new PatchUserRequest(tooLongFirstName, null, null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithTooLongLastNameReturnsBadRequest() {
        var email = "patchlong2@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "Long");

        var tooLongLastName = new string('a', 101);
        var patchRequest = new PatchUserRequest(null, tooLongLastName, null);
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchUserWithTooShortPasswordReturnsBadRequest() {
        var email = "patchpwdshort@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "Patch", "PwdShort");

        var patchRequest = new PatchUserRequest(null, null, "short");
        using var patchMessage = TestHelpers.CreateAuthenticatedPatchRequest(UriProvider.GetUserUrl(userId), accessToken, patchRequest);
        var response = await _client.SendAsync(patchMessage, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}

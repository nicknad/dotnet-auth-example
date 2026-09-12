using Auth.Api.Features.Users.Register;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Users;

public sealed class RegisterTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public RegisterTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task RegisterWithValidDataReturnsOk() {
        // Arrange
        var email = "test@example.com";

        // Act
        var response = await TestHelpers.RegisterAsync(_client, email, "Password123!", "Password123!", "Test", "User");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>(CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task RegisterWithTooLongFirstNameReturnsBadRequest() {
        var longName = new string('a', 101);
        var response = await TestHelpers.RegisterAsync(_client, "longfn@test.com", "Password123!", "Password123!", longName, "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithTooLongEmailReturnsBadRequest() {
        var longEmail = new string('a', 250) + "@test.com";
        var response = await TestHelpers.RegisterAsync(_client, longEmail, "Password123!", "Password123!", "First", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithInvalidEmailReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "invalid-email", "Password123!", "Password123!", "First", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithDifferingPasswordsReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "user@test.com", "Password123!", "DifferentPassword123!", "First", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithWeakPasswordReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "user@test.com", "weak", "weak", "First", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithDuplicateEmailReturnsBadRequest() {
        var email = "duplicate@test.com";
        await TestHelpers.RegisterAsync(_client, email, "Password123!", "Password123!", "First", "Last");
        var response2 = await TestHelpers.RegisterAsync(_client, email, "Password123!", "Password123!", "First", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task RegisterWithEmptyFirstNameReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "emptyfn@test.com", "Password123!", "Password123!", "", "Last");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithEmptyLastNameReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "emptyln@test.com", "Password123!", "Password123!", "First", "");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithSqlInjectionInFirstNameReturnsOk() {
        var response = await TestHelpers.RegisterAsync(_client, "sqli1@test.com", "Password123!", "Password123!", "'; DROP TABLE users; --", "Last");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var response2 = await TestHelpers.LoginAsync(_client, "sqli1@test.com", "Password123!");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task RegisterWithTooLongLastNameReturnsBadRequest() {
        var longLastName = new string('a', 101);
        var response = await TestHelpers.RegisterAsync(_client, "longln@test.com", "Password123!", "Password123!", "First", longLastName);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithEmptyPasswordReturnsBadRequest() {
        var response = await TestHelpers.RegisterAsync(_client, "emptypwd@test.com", "", "", "", "");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterAsAdminReturnsOk() {
        var email = "newadmin@test.com";
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        var roles = new List<string> { "Admin" };

        var response = await TestHelpers.RegisterAsync(_client, email, "Password123!", "Password123!", "New", "Admin", roles, adminToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify user can login and has admin claims
        var loginToken = await TestHelpers.LoginAndGetTokenAsync(_client, email, "Password123!");
        Assert.NotNull(loginToken);
        
        // Use ListUsers as a proxy for checking admin role
        using var listRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, loginToken);
        var listResponse = await _client.SendAsync(listRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task RegisterWithNullEmailReturnsBadRequest() {
        using var request = new HttpRequestMessage(HttpMethod.Post, UriProvider.RegisterUrl);
        request.Content = JsonContent.Create(new {
            email = (string?)null,
            password = "Password123!",
            confirmPassword = "Password123!",
            firstName = "Null",
            lastName = "Email",
        });
        var response = await _client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithNullNamesReturnsBadRequest() {
        using var request = new HttpRequestMessage(HttpMethod.Post, UriProvider.RegisterUrl);
        request.Content = JsonContent.Create(new {
            email = "nullnames@test.com",
            password = "Password123!",
            confirmPassword = "Password123!",
            firstName = (string?)null,
            lastName = (string?)null,
        });
        var response = await _client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWithUnknownRoleReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        var roles = new List<string> { "SuperAdmin" };

        var response = await TestHelpers.RegisterAsync(_client, "unknownrole@test.com", "Password123!", "Password123!", "Unknown", "Role", roles, adminToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDeletedUserRevivesAccount() {
        var email = "revive@test.com";
        var (userId, accessToken) = await TestHelpers.RegisterAndLoginAsync(_client, email, "First", "Last");

        // Soft delete the user
        using var deleteRequest = TestHelpers.CreateAuthenticatedDeleteRequest(UriProvider.GetUserUrl(userId), accessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Re-register with the same email
        var response = await TestHelpers.RegisterAsync(_client, email, "NewPassword123!", "NewPassword123!", "Revived", "User");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>(CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id); // Should be the same ID

        // Verify can login with new password
        var loginToken = await TestHelpers.LoginAndGetTokenAsync(_client, email, "NewPassword123!");
        Assert.NotNull(loginToken);
    }
}

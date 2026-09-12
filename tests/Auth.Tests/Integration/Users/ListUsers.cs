using Auth.Api.Features.Users.GetById;
using System.Net;
using System.Net.Http.Json;

namespace Auth.Tests.Integration.Users;

public sealed class ListUsers : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public ListUsers(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task ListUsersWhenAdminReturnsAllUsers() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, adminToken);

        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public async Task ListUsersWhenNormalUserReturnsOwnProfile() {
        var email = "listuser@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "List", "User");
        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email, "Password123!");

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, accessToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Single(users);
        Assert.Equal(email, users[0].Email);
    }

    [Fact]
    public async Task ListUsersWithNameFilterReturnsMatchingUsers() {
        await TestHelpers.RegisterTestUserAsync(_client, "namefilter1@test.com", "John", "Doe");
        await TestHelpers.RegisterTestUserAsync(_client, "namefilter2@test.com", "Jane", "Smith");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new() { ["name"] = "John" }),
            adminToken);

        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Single(users);
    }

    [Fact]
    public async Task ListUsersWithEmailFilterReturnsMatchingUsers() {
        await TestHelpers.RegisterTestUserAsync(_client, "emailfilter1@test.com", "Email", "Filter1");
        await TestHelpers.RegisterTestUserAsync(_client, "emailfilter2@test.com", "Email", "Filter2");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new() { ["email"] = "emailfilter1@test.com" }),
            adminToken);

        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Single(users);
    }

    [Fact]
    public async Task ListUsersWithPaginationReturnsCorrectSubset() {
        for (int i = 0; i < 5; i++) {
            var email = $"pageuser{i}@test.com";
            await TestHelpers.RegisterTestUserAsync(_client, email, "Page", $"User{i}");
        }

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, 2),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
    }

    /// <summary>
    /// Verifies that an authenticated admin can list users.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ListWhenAdminAuthenticatedReturnsUsers() {
        // Arrange
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, adminToken);

        var response = await _client.SendAsync(getRequest, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    /// <summary>
    /// Verifies that a normal user can see their own profile in the list.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task ListWhenNormalUserAuthenticatedReturnsOwnProfile() {
        // Arrange
        var email = "normal@example.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Jane", "Doe");
        var accessToken = await TestHelpers.LoginAndGetTokenAsync(_client, email, "Password123!");

        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(UriProvider.ListUsersUrl, accessToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Single(users);
        Assert.Equal(email, users[0].Email);
    }

    [Fact]
    public async Task ListUsersUnauthenticatedReturnsUnauthorized() {
        var response = await _client.GetAsync(UriProvider.ListUsersUrl, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithExcessivePageSizeReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, 10000),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithMaxPageSizeReturnsOk() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, 50),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithNegativePageNumberReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), -1, 10),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithNegativePageSizeReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, -1),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithZeroPageSizeReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, 0),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithVeryLongFilterValueReturnsBadRequest() {
        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        var longFilterValue = new string('a', 500);
        using var filterRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new() { ["name"] = longFilterValue }),
            adminToken);
        var filterResponse = await _client.SendAsync(filterRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, filterResponse.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithMultipleFiltersReturnsFilteredResults() {
        await TestHelpers.RegisterTestUserAsync(_client, "multifilter1@test.com", "Alice", "Smith");
        await TestHelpers.RegisterTestUserAsync(_client, "multifilter2@test.com", "Bob", "Smith");

        var adminToken = await TestHelpers.GetAdminTokenAsync(_client);
        using var getRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(new(), 0, 10),
            adminToken);
        var response = await _client.SendAsync(getRequest, CancellationToken.None);

        var filters = new Dictionary<string, string?> { ["name"] = "Alice", ["email"] = "multifilter1" };
        using var filterRequest = TestHelpers.CreateAuthenticatedGetRequest(
            UriProvider.GetListUsersWithQueryParams(filters),
            adminToken);
        var filterResponse = await _client.SendAsync(filterRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, filterResponse.StatusCode);
        var users = await filterResponse.Content.ReadFromJsonAsync<List<GetUserResponse>>(CancellationToken.None);
        Assert.NotNull(users);
        Assert.Single(users);
    }
}

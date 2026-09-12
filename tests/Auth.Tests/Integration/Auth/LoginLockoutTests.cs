using System.Net;

namespace Auth.Tests.Integration.Auth;

public sealed class LoginLockoutTests : IClassFixture<IntegrationTestFixture>
{
    private const string CorrectPassword = "Password123!";

    private readonly HttpClient _client;

    public LoginLockoutTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task LoginLocksAccountAfterRepeatedFailedAttempts() {
        var email = "lockout@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Lock", "Out");

        for (var i = 0; i < 5; i++) {
            var failed = await TestHelpers.LoginAsync(_client, email, "WrongPassword1!");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var lockedOut = await TestHelpers.LoginAsync(_client, email, CorrectPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, lockedOut.StatusCode);
    }

    [Fact]
    public async Task SuccessfulLoginResetsFailedAttempts() {
        var email = "lockoutreset@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Lock", "Reset");

        var failed = await TestHelpers.LoginAsync(_client, email, "WrongPassword1!");
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);

        var success = await TestHelpers.LoginAsync(_client, email, CorrectPassword);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        var failedAgain = await TestHelpers.LoginAsync(_client, email, "WrongPassword1!");
        Assert.Equal(HttpStatusCode.Unauthorized, failedAgain.StatusCode);

        var successAgain = await TestHelpers.LoginAsync(_client, email, CorrectPassword);
        Assert.Equal(HttpStatusCode.OK, successAgain.StatusCode);
    }
}

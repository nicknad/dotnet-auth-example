using Auth.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Tests.Integration.Auth;

public sealed class RefreshTokenStorageTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public RefreshTokenStorageTests(IntegrationTestFixture fixture) {
        ArgumentNullException.ThrowIfNull(fixture, nameof(fixture));
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task RefreshTokensAreStoredAsHashesOnly() {
        var email = "hashedrefresh@test.com";
        await TestHelpers.RegisterTestUserAsync(_client, email, "Hashed", "Refresh");
        var refreshToken = await TestHelpers.GetRefreshTokenAsync(_client, email, "Password123!");

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);

        Assert.NotNull(user.RefreshToken);
        Assert.NotEqual(refreshToken, user.RefreshToken);
        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        Assert.Equal(expectedHash, user.RefreshToken);
    }
}

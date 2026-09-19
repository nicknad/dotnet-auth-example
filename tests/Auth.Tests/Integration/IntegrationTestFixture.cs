using Auth.Api.Common;
using Auth.Api.Features.Users.Register;
using Auth.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;


namespace Auth.Tests.Integration;

#pragma warning disable CA1515 // Consider making public types internal
public sealed class IntegrationTestFixture : IAsyncLifetime
#pragma warning restore CA1515 // Consider making public types internal
{
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;
    public HttpClient HttpClient { get; private set; } = default!;
    public TestTimeProvider TimeProvider { get; } = new();

    public async ValueTask InitializeAsync() {
        var dbId = Guid.NewGuid().ToString("N");

        // Fail-closed JWT requires an explicit key. WebApplicationFactory's
        // ConfigureAppConfiguration merge timing is version-sensitive, so set the
        // test key via env (picked up by the default env provider) as well.
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-jwt-signing-key-32-plus-characters-0123456789");

#pragma warning disable CA2000 // Dispose objects before losing scope
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Note: Jwt:Key is supplied via environment (see below): extra
                    // Jwt entries here would duplicate appsettings.json values.
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "NoRateLimit", "true" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase($"TestDb_{dbId}"));

                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(TimeProvider);
                });
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

        HttpClient = Factory.CreateClient();

        await SeedDatabase();
    }

    public async ValueTask DisposeAsync() {
        HttpClient.Dispose();
        Factory.Dispose();
    }

    private async Task SeedDatabase() {
        using var scope = Factory.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "User" }) {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = "admin@example.com";

        if (await userManager.FindByEmailAsync(adminEmail) == null) {
            var adminUser = new ApplicationUser {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Extensions;
using Auth.Api.Features.Auth;
using Auth.Api.Features.Health;
using Auth.Api.Features.Users;
using Auth.Api.Infrastructure.Services;
using Auth.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Auth.Tests.Security;

#pragma warning disable CA1515 // Consider making public types internal
public sealed class KestrelFixture : IAsyncLifetime
#pragma warning restore CA1515 // Consider making public types internal
{
    public HttpClient Client { get; private set; } = default!;
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string BaseUrl { get; private set; } = default!;
#pragma warning restore CA1056 // URI-like properties should not be strings

    private WebApplication? _app;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
        });

        // Configure Serilog for testing
        builder.Host.UseSerilog((ctx, lc) => lc
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Auth.Api"));

        // Configure Kestrel limits
        builder.ConfigureKestrelLimits();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // Use extension methods for DI setup
        builder.Services.AddTestDatabase();
        builder.Services.AddApplicationIdentity();
        builder.Services.AddCoreServices();  // This includes ITokenHandler, ICacheService, IpBlockingService, etc.

        // Authentication & JWT setup using extension method
        builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddAuthorization();

        // CORS and security headers, matching the application pipeline
        builder.Services.AddApplicationCors(builder.Configuration, builder.Environment);
        builder.Services.AddApplicationSecurityHeaders();

        // Add rate limiting using extension method
        builder.Services.AddApplicationRateLimiting();

        // Add auth logger with console output for testing
        var authSerilog = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Auth.Api")
            .CreateLogger();
        builder.Services.AddAuthLogger(authSerilog);

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        await SeedTestDatabase(_app);

        // Use the same middleware pipeline and routes as the real application
        _app.UseApplicationPipeline();
        _app.MapApplicationRoutes();

        await _app.StartAsync();

        BaseUrl = _app.Urls.First();

        Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    private static async Task SeedTestDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        var adminEmail = "admin@example.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            var createdUser = await userManager.FindByEmailAsync(adminEmail);
            if (createdUser != null)
            {
                await userManager.AddToRoleAsync(createdUser, "Admin");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

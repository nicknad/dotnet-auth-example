using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Extensions;
using Auth.Api.Features.Auth;
using Auth.Api.Features.Health;
using Auth.Api.Features.Users;
using Auth.Api.Infrastructure.Storage;
using Auth.Api.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Auth.Api")
    .CreateLogger();

try
{
    Log.Information("Starting Auth API...");

    #region logging setup
    builder.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Auth.Api"));

    var authSerilog = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File("../../Logs/auth-log.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Auth.Api")
        .CreateLogger();

    builder.Services.AddAuthLogger(authSerilog);
    #endregion

    // Configure Kestrel server limits
    builder.ConfigureKestrelLimits();

    // Add application services using extension methods
    builder.Services.AddApplicationDatabase(builder);
    builder.Services.AddApplicationIdentity();
    builder.Services.AddCoreServices();
    builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddApplicationCors(builder.Configuration, builder.Environment);
    builder.Services.AddApplicationSecurityHeaders();
    builder.Services.AddApplicationRateLimiting();

    var app = builder.Build();

    // Read command-line flags
    bool noRateLimit = app.Configuration.GetValue<bool>("NoRateLimit") ||
                     app.Configuration.GetValue<bool>("no-rate-limit");

    bool seedDb = app.Configuration.GetValue<bool>("SeedDb") ||
                  app.Configuration.GetValue<bool>("seed-db");

    #region seed database
    if (seedDb)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ApplicationDbContext>();

            if (!app.Environment.IsEnvironment("Testing")) {
                await db.Database.MigrateAsync();
            }

            await DatabaseUtil.SeedDatabase(services);
            Log.Information("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    if (!app.Environment.IsEnvironment("Testing")) {
        using (var scope = app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        }
    }
    #endregion

    // Configure the application pipeline
    app.UseApplicationPipeline(useRateLimiting: !noRateLimit);

    // Map all API routes
    app.MapApplicationRoutes(requireRateLimiting: !noRateLimit);

    Log.Information("Application started successfully.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start or crashed unexpectedly.");
    // TODO: Add notification (email, Slack, Teams, etc.)
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Partial Program class for WebApplicationFactory.
/// </summary>
[SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Needed for WebApplicationFactory")]
#pragma warning disable CA1515 // Consider making public types internal
public partial class Program
#pragma warning restore CA1515 // Consider making public types internal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// </summary>
    protected Program()
    {
    }
}

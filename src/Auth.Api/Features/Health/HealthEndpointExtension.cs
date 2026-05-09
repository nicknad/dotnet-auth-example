using System.Net;

namespace Auth.Api.Features.Health;

internal static class HealthEndpointExtension
{
    // Health checks
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app) {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
          .Produces<HealthStatus>()
          .WithName("Health")
          .WithDescription("Get the health status of the application. Useful for monitoring and scaling.");
    }
}

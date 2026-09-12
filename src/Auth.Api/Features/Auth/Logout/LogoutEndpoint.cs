using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Auth.Api.Features.Auth.Logout;

/// <summary>
/// Contains the endpoint for user logout.
/// </summary>
internal static class LogoutEndpoint
{
    /// <summary>
    /// Maps the logout endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapLogout(this IEndpointRouteBuilder app) {
        app.MapPost("/auth/logout", async (
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequest? request,
            ClaimsPrincipal user,
            [FromServices] ITokenHandler tokenHandler) =>
        {
            // Try both "sub" and NameIdentifier claims
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) {
                return Results.Unauthorized();
            }

            var revokeAllTokens = request?.RevokeAllTokens ?? false;
            var result = await tokenHandler.LogoutAsync(userId, revokeAllTokens).ConfigureAwait(false);

            if (result.Succeeded) {
                return Results.Ok(new { message = "Logged out successfully" });
            }

            if (result.Errors.Contains("User not found")) {
                return Results.Unauthorized();
            }

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        })
        .WithName("Logout")
        .Accepts<LogoutRequest>("application/json")
        .Produces((int)System.Net.HttpStatusCode.OK)
        .Produces((int)System.Net.HttpStatusCode.Unauthorized);
    }
}

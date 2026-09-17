using Auth.Api.Abstractions;
using Microsoft.AspNetCore.Mvc;
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
        // Note: the request body (LogoutRequest.RevokeAllTokens) is accepted for
        // wire compatibility but ignored: logout always revokes all tokens.
        app.MapPost("/auth/logout", async (
            ClaimsPrincipal user,
            [FromServices] ITokenHandler tokenHandler) =>
        {
            // Try both "sub" and NameIdentifier claims
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) {
                return Results.Unauthorized();
            }

            var result = await tokenHandler.LogoutAsync(userId).ConfigureAwait(false);

            if (result.Succeeded) {
                return Results.Ok(new { message = "Logged out successfully" });
            }

            if (result.Errors.Contains("User not found")) {
                return Results.Unauthorized();
            }

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        })
        .WithName("Logout")
        .RequireAuthorization()
        .Accepts<LogoutRequest>("application/json")
        .Produces((int)System.Net.HttpStatusCode.OK)
        .Produces((int)System.Net.HttpStatusCode.Unauthorized);
    }
}

using Auth.Api.Abstractions;
using Auth.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Features.Users.Delete;

/// <summary>
/// Contains the endpoint for soft-deleting a user.
/// </summary>
internal static class DeleteUserEndpoint
{
    /// <summary>
    /// Maps the DeleteUser endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapDeleteUser(this IEndpointRouteBuilder app) {
        app.MapDelete("/users/{id}", async (
            [FromRoute] string id,
            ClaimsPrincipal user,
            IUserStorage userStorage,
            ICacheService cache) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole(Common.Constants.Roles.Admin);

            if (currentUserId != id && !isAdmin) {
                return Results.Forbid();
            }

            var result = await userStorage.DeleteAsync(id);

            if (result.Succeeded) {
                // Invalidate the user's validation cache to prevent accessing with old tokens
                cache.Remove(Common.CacheKeys.UserValidation(id));
                return Results.NoContent();
            }

            if (result.Errors.Contains("User not found") || result.Errors.Contains("User is already deleted")) {
                return Results.NotFound();
            }

            return Results.BadRequest(result.Errors);
        })
        .WithName("DeleteUser")
        .RequireAuthorization()
        .Produces((int)System.Net.HttpStatusCode.NoContent)
        .Produces((int)System.Net.HttpStatusCode.NotFound)
        .Produces((int)System.Net.HttpStatusCode.Forbidden);
    }
}

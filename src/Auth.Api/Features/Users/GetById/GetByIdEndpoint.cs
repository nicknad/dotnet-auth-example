using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Auth.Api.Features.Users.GetById;

/// <summary>
/// Contains the endpoint for getting a user by ID.
/// </summary>
internal static class GetByIdEndpoint
{
    /// <summary>
    /// Maps the GetById endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapGetUserById(this IEndpointRouteBuilder app) {
        app.MapGet("/users/{id}", async (
            string id,
            ClaimsPrincipal user,
            IUserStorage userStorage) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole(Common.Constants.Roles.Admin);

            if (currentUserId != id && !isAdmin) {
                return Results.Forbid();
            }

            var targetUser = await userStorage.FindByIdAsync(id).ConfigureAwait(false);

            if (targetUser == null) {
                return Results.NotFound();
            }

            return Results.Ok(new GetUserResponse(
            targetUser.Id,
            targetUser.Email ?? string.Empty,
            targetUser.FirstName ?? string.Empty,
            targetUser.LastName ?? string.Empty));
        })
        .WithName("GetUserById")
        .RequireAuthorization()
        .Produces<GetUserResponse>()
        .Produces((int)System.Net.HttpStatusCode.NotFound)
        .Produces((int)System.Net.HttpStatusCode.Forbidden);
    }
}

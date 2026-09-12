using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.InputValidation;
using Auth.Api.Features.Users.GetById;
using Auth.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Features.Users.Patch;

/// <summary>
/// Contains the endpoint for updating user data.
/// </summary>
internal static class PatchUserEndpoint
{
    /// <summary>
    /// Maps the PatchUser endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapPatchUser(this IEndpointRouteBuilder app) {
        app.MapPatch("/users/{id}", async (
            string id,
            [FromBody] PatchUserRequest request,
            ClaimsPrincipal user,
            IUserStorage userStorage,
            ICacheService cache) =>
        {
            if (request.IsEmpty()) {
                return Results.BadRequest("At least one field must be provided for update.");
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole(Common.Constants.Roles.Admin);

            if (currentUserId != id && !isAdmin) {
                return Results.Forbid();
            }

            var targetUser = await userStorage.FindByIdAsync(id);

            if (targetUser == null) {
                return Results.NotFound();
            }

            if (!string.IsNullOrEmpty(request.Password)) {
                var passwordValidatorResult = PasswordValidator.Validate(request.Password.AsSpan());
                if (!passwordValidatorResult.IsValid) {
                    return Results.BadRequest(passwordValidatorResult.Error);
                }
            }

            if (!string.IsNullOrEmpty(request.FirstName)) {
                targetUser.FirstName = request.FirstName;
            }

            if (!string.IsNullOrEmpty(request.LastName)) {
                targetUser.LastName = request.LastName;
            }

            var updateResult = await userStorage.UpdateAsync(targetUser);

            if (!updateResult.Succeeded) {
                return Results.BadRequest(updateResult.Errors);
            }

            if (!string.IsNullOrEmpty(request.Password)) {
                var passwordUpdateResult = await userStorage.UpdatePasswordAsync(targetUser, request.Password);

                if (!passwordUpdateResult.Succeeded) {
                    return Results.BadRequest(passwordUpdateResult.Errors);
                }

                // Invalidate cache to force re-authentication with new token version
                cache.Remove(CacheKeys.UserValidation(id));
            }

            return Results.Ok(new GetUserResponse(
                targetUser.Id,
                targetUser.Email ?? string.Empty,
                targetUser.FirstName ?? string.Empty,
                targetUser.LastName ?? string.Empty));
        })
        .WithName("PatchUser")
        .RequireAuthorization()
        .Accepts<PatchUserRequest>("application/json")
        .Produces<GetUserResponse>()
        .Produces((int)System.Net.HttpStatusCode.NotFound)
        .Produces((int)System.Net.HttpStatusCode.BadRequest)
        .Produces((int)System.Net.HttpStatusCode.Forbidden);
    }
}

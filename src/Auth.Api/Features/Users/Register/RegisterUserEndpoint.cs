using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Features.Users.Register;

/// <summary>
/// Contains the endpoint for user registration.
/// </summary>
internal static class RegisterUserEndpoint
{
    /// <summary>
    /// Maps the registration endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapRegisterUser(this IEndpointRouteBuilder app) {
        app.MapPost("/users/register", async (
            [FromBody] RegisterUserRequest request,
            [FromServices] IUserStorage userStorage,
            ICacheService cache,
            ClaimsPrincipal currentUser) =>
        {
            var existingUser = await userStorage.FindByEmailAsync(request.Email);
            
            // Check if email is already taken by a NON-DELETED user
            if (existingUser != null && !existingUser.IsDeleted) {
                return Results.BadRequest("Email already registered");
            }

            IReadOnlyList<string> roles = new List<string> { "User" };
            bool isAdminRequester = currentUser.Identity?.IsAuthenticated == true && 
                                  currentUser.IsInRole(Common.Constants.Roles.Admin);

            // Only admins can specify roles
            if (isAdminRequester && request.Roles != null && request.Roles.Count > 0) 
            {
                roles = request.Roles;
            }

            AuthApiResult result;
            if (existingUser != null && existingUser.IsDeleted) {
                // "Revive" or replace the deleted user
                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.IsDeleted = false;
                existingUser.IsActive = true;
                existingUser.TokenVersion++;
                existingUser.RefreshToken = null;
                existingUser.RefreshTokenExpiresAt = null;
                
                // We need to reset password and roles
                result = await userStorage.UpdateAsync(existingUser);

                if (result.Succeeded) {
                    result = await userStorage.UpdatePasswordAsync(existingUser, request.Password);
                }

                if (result.Succeeded) {
                    // Reset roles: remove all and add new
                    var currentRoles = await userStorage.GetRolesByUserAsync(existingUser);
                    foreach (var role in currentRoles) {
                        result = await userStorage.RemoveRole(existingUser.Id, role);

                        if (!result.Succeeded) {
                            break;
                        }
                    }
                }

                if (result.Succeeded) {
                    foreach (var role in roles) {
                        result = await userStorage.AddRole(existingUser.Id, role);

                        if (!result.Succeeded) {
                            break;
                        }
                    }
                }
            } else {
                var user = new ApplicationUser {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true
                };
                result = await userStorage.CreateAsync(user, request.Password, roles.ToList());
                existingUser = user; // for the response
            }

            if (!result.Succeeded) {
                return Results.BadRequest(result.Errors);
            }

            // A revived account must not be blocked by a stale cached "deleted/inactive" entry.
            cache.Remove(Common.CacheKeys.UserValidation(existingUser.Id));

            return Results.Ok(new RegisterUserResponse(existingUser.Id, existingUser.Email!));
        })
        .WithName("RegisterUser")
        .Accepts<RegisterUserRequest>("application/json")
        .Produces<RegisterUserResponse>()
        .Produces((int)System.Net.HttpStatusCode.BadRequest);
    }
}

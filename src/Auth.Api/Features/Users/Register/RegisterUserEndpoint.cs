using Auth.Api.Abstractions;
using Auth.Api.Common;
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
            ClaimsPrincipal currentUser) =>
        {
            var existingUser = await userStorage.FindByEmailAsync(request.Email);

            // Tombstone deleted emails: never auto-revive on anonymous register.
            // Otherwise anyone could claim a deleted address without ownership proof
            // (no email verification) and inherit the same user Id.
            // Restoration requires an explicit admin/support flow with verification.
            if (existingUser != null) {
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
            {
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

            return Results.Ok(new RegisterUserResponse(existingUser.Id, existingUser.Email!));
        })
        .WithName("RegisterUser")
        .Accepts<RegisterUserRequest>("application/json")
        .Produces<RegisterUserResponse>()
        .Produces((int)System.Net.HttpStatusCode.BadRequest);
    }
}

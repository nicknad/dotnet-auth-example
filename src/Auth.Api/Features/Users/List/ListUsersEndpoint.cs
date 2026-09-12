using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Features.Users.GetById;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Features.Users.List;

internal static class ListUsersEndpoint
{
    public static void MapListUsers(this IEndpointRouteBuilder app) {
        app.MapGet("/users", async (
            [AsParameters] UserQueryParameters parameters,
            ClaimsPrincipal user,
            IUserStorage userStorage) =>
        {
            var validationError = parameters.Validate();
            if (validationError is not null) {
                return Results.BadRequest(validationError);
            }

            var isAdmin = user.IsInRole(Common.Constants.Roles.Admin);
            
            List<ApplicationUser> users;
            if (isAdmin) {
                users = await userStorage.ListAsync(parameters);
            } else {
                var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var singleUser = await userStorage.FindByIdAsync(currentUserId!);
                users = singleUser != null ? [singleUser] : [];
            }

            var response = users.Select(u => new GetUserResponse(
                u.Id,
                u.Email ?? string.Empty,
                u.FirstName ?? string.Empty,
                u.LastName ?? string.Empty))
                .ToList();

            return Results.Ok(response);
        })
        .WithName("ListUsers")
        .RequireAuthorization()
        .Produces<List<GetUserResponse>>()
        .Produces((int)System.Net.HttpStatusCode.Forbidden);
    }
}

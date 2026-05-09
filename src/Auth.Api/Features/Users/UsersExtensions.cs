using Auth.Api.Features.Users.Delete;
using Auth.Api.Features.Users.GetById;
using Auth.Api.Features.Users.List;
using Auth.Api.Features.Users.Patch;
using Auth.Api.Features.Users.Register;

namespace Auth.Api.Features.Users;

internal static class UsersExtensions
{
    public static void MapUsers(this IEndpointRouteBuilder app) {
        app.MapRegisterUser();
        app.MapGetUserById();
        app.MapListUsers();
        app.MapPatchUser();
        app.MapDeleteUser();
    }
}

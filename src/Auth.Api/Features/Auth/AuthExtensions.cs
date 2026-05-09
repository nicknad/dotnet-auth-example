using Auth.Api.Features.Auth.Login;
using Auth.Api.Features.Auth.Logout;
using Auth.Api.Features.Auth.Refresh;

namespace Auth.Api.Features.Auth;

internal static class AuthExtensions
{
    public static void MapAuth(this IEndpointRouteBuilder app) {
        app.MapLogin();
        app.MapRefreshToken();
        app.MapLogout();
    }
}

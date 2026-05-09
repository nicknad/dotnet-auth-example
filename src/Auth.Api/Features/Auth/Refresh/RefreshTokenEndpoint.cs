using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.Token;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Features.Auth.Refresh;

/// <summary>
/// Contains the endpoint for refreshing access tokens.
/// </summary>
internal static class RefreshTokenEndpoint
{
    /// <summary>
    /// Maps the refresh token endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapRefreshToken(this IEndpointRouteBuilder app) {
        app.MapPost("/auth/refresh", async (
            [FromBody] RefreshTokenRequest request,
            [FromServices] ITokenHandler tokenHandler) =>
        {
            var result = await tokenHandler.RefreshTokenAsync(request.RefreshToken);

            return result.Status switch
            {
                TokenResultStatus.Success => Results.Ok(new RefreshTokenResponse(
                    result.Token!.Token,
                    result.Token.RefreshToken,
                    result.Token.ExpiresAt)),
                TokenResultStatus.InvalidCredentials => Results.Unauthorized(),
                TokenResultStatus.UserNotActive => Results.Unauthorized(),
                TokenResultStatus.UserDeleted => Results.Unauthorized(),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        })
        .WithName("RefreshToken")
        .Accepts<RefreshTokenRequest>("application/json")
        .Produces<RefreshTokenResponse>()
        .Produces((int)System.Net.HttpStatusCode.Unauthorized);
    }
}

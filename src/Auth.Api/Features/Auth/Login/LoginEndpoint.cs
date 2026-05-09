using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.Token;
using Auth.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Features.Auth.Login;

/// <summary>
/// Contains the endpoint for user login.
/// </summary>
internal static class LoginEndpoint
{
    /// <summary>
    /// Maps the login endpoint to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapLogin(this IEndpointRouteBuilder app) {
        app.MapPost("/auth/login", async (
            [FromBody] LoginRequest request,
            ITokenHandler tokenHandler,
            ClaimsPrincipal currentUser) =>
        {
            var email = request.Email;
            var tokenResult = await tokenHandler.ValidateLoginAndCreateTokenAsync(request.Email, request.Password);

            return tokenResult.Status switch  {
                TokenResultStatus.Success => Results.Ok(new LoginResponse(tokenResult.Token!.Token, tokenResult.Token!.RefreshToken, email, tokenResult.Token!.ExpiresAt)),
                TokenResultStatus.InvalidCredentials => Results.Unauthorized(),
                TokenResultStatus.UserNotFound => Results.Unauthorized(),
                TokenResultStatus.UserNotActive => Results.Unauthorized(),
                TokenResultStatus.UserDeleted => Results.Unauthorized(),
                _ => Results.InternalServerError()
            };
        })
        .WithName("Login")
        .Accepts<LoginRequest>("application/json")
        .Produces<LoginResponse>()
        .Produces((int)System.Net.HttpStatusCode.OK)
        .Produces((int)System.Net.HttpStatusCode.Unauthorized);
    }
}

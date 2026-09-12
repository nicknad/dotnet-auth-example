#pragma warning disable CA5404, CA1849
using BenchmarkDotNet.Attributes;
using Auth.Api.Infrastructure.Services;
using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.Token;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NSubstitute;

namespace Auth.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class ClaimsExtractionBench
#pragma warning restore CA1515 // Consider making public types internal
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private ClaimsPrincipal _principal = null!;

    [GlobalSetup]
    public async Task Setup() {
        var keyString = "super-secret-key-12345-super-secret-key-12345";
        var issuer = "test-issuer";
        var audience = "test-audience";

        var options = Options.Create(new JwtOptions {
            Issuer = issuer,
            Audience = audience,
            Key = keyString,
        });

        var userStorage = Substitute.For<IUserStorage>();
        userStorage.GetRolesByUserAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult((IList<string>)new List<string> { "User" }));
        
        var logger = Substitute.For<IAuthLogger>();
        var cache = Substitute.For<ICacheService>();

        var tokenHandler = new Auth.Api.Infrastructure.Services.TokenHandler(options, userStorage, logger, TimeProvider.System, cache);
        var user = new ApplicationUser { Id = "user-1", Email = "test@example.com", TokenVersion = 1, IsActive = true };
        
        var result = await tokenHandler.GenerateJwtToken(user);
        var token = result.Token;

        var parameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        _principal = _handler.ValidateToken(token, parameters, out _);
    }

    [Benchmark]
    public string? GetUserId() {
        return _principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    [Benchmark]
    public string? GetTokenVersion() {
        return _principal.FindFirst("ver")?.Value;
    }
}
#pragma warning restore CA5404

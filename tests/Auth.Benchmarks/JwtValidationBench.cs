#pragma warning disable CA5404
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
public class JwtValidationBench
#pragma warning restore CA1515 // Consider making public types internal
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private string _token = string.Empty;
    private TokenValidationParameters _parameters = null!;

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
        _token = result.Token;

        _parameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    [Benchmark]
    public ClaimsPrincipal Validate() {
        return _handler.ValidateToken(_token, _parameters, out _);
    }
}
#pragma warning restore CA5404

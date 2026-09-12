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
public class TokenVersionCheckBench
#pragma warning restore CA1515 // Consider making public types internal
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private string _token = string.Empty;
    private TokenValidationParameters _parameters = null!;
    private ICacheService _cache = null!;
    private IUserStorage _userStorage = null!;
    private ApplicationUser _user = null!;

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

        _user = new ApplicationUser { Id = "user-1", Email = "test@example.com", TokenVersion = 1, IsActive = true };
        
        _userStorage = Substitute.For<IUserStorage>();
        _userStorage.GetRolesByUserAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult((IList<string>)new List<string> { "User" }));
        _userStorage.FindByIdAsync(_user.Id).Returns(Task.FromResult((ApplicationUser?)_user));
        
        var logger = Substitute.For<IAuthLogger>();
        _cache = Substitute.For<ICacheService>();

        var tokenHandler = new Auth.Api.Infrastructure.Services.TokenHandler(options, _userStorage, logger, TimeProvider.System, _cache);
        
        var result = await tokenHandler.GenerateJwtToken(_user);
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

        // Pre-fill cache for CacheHit benchmark
        var cacheKey = $"user:{_user.Id}:validation";
        var cacheEntry = new UserCacheEntry(_user.TokenVersion, _user.IsActive);
        _cache.TryRetrieve<UserCacheEntry>(cacheKey, out Arg.Any<UserCacheEntry>())
              .Returns(x => { x[1] = cacheEntry; return true; });
    }

    [Benchmark]
    public async Task<bool> CacheHit() {
        var principal = _handler.ValidateToken(_token, _parameters, out _);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenVersionClaim = principal.FindFirst("ver")?.Value;

        var cacheKey = $"user:{userId}:validation";
        if (_cache.TryRetrieve<UserCacheEntry>(cacheKey, out var cacheEntry)) {
            return cacheEntry.TokenVersion.ToString() == tokenVersionClaim;
        }
        return false;
    }

    [Benchmark]
    public async Task<bool> DbLookup() {
        // Mock cache miss for this benchmark
        var principal = _handler.ValidateToken(_token, _parameters, out _);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenVersionClaim = principal.FindFirst("ver")?.Value;

        // Simulate logic from TokenValidationMiddleware
        var user = await _userStorage.FindByIdAsync(userId!);
        if (user == null) return false;

        var cacheEntry = new UserCacheEntry(user.TokenVersion, user.IsActive);
        // In real middleware, we would store it in cache here.
        
        return cacheEntry.TokenVersion.ToString() == tokenVersionClaim;
    }
}
#pragma warning restore CA5404, CA1849

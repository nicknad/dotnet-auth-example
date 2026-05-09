using BenchmarkDotNet.Attributes;
using Auth.Api.Infrastructure.Services;
using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Auth.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class JwtCreationBench
#pragma warning restore CA1515 // Consider making public types internal
{
    private Auth.Api.Infrastructure.Services.TokenHandler _handler = null!;
    private ApplicationUser _user = null!;

    [GlobalSetup]
    public void Setup() {
        var config = Substitute.For<IConfiguration>();
        config["JWT:Issuer"].Returns("test-issuer");
        config["JWT:Audience"].Returns("test-audience");
        config["JWT:Key"].Returns("super-secret-key-12345-super-secret-key-12345");

        var userStorage = Substitute.For<IUserStorage>();
        userStorage.GetRolesByUserAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult((IList<string>)new List<string> { "User" }));

        var logger = Substitute.For<IAuthLogger>();
        var cache = Substitute.For<ICacheService>();

        _handler = new Auth.Api.Infrastructure.Services.TokenHandler(config, userStorage, logger, TimeProvider.System, cache);
        _user = new ApplicationUser { Id = "user-1", Email = "test@example.com", TokenVersion = 1, IsActive = true };
    }

    [Benchmark]
    public async Task<string> CreateToken() {
        var result = await _handler.GenerateJwtToken(_user);
        return result.Token;
    }
}

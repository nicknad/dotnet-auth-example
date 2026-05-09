using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Identity;
using Auth.Api.Common.InputValidation;

namespace Auth.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class PasswordHashBench
#pragma warning restore CA1515 // Consider making public types internal
{
    private readonly PasswordHasher<object> _hasher = new();
    private string _hash = string.Empty;
    private const string Password = "P@ssw0rd123!";

    [GlobalSetup]
    public void Setup() {
        _hash = _hasher.HashPassword(null!, Password);
    }

    [Benchmark]
    public PasswordVerificationResult Verify() {
        return _hasher.VerifyHashedPassword(null!, _hash, Password);
    }

    [Benchmark]
#pragma warning disable CA1822 // Mark members as static
    public bool Validate() {
#pragma warning restore CA1822 // Mark members as static
        return PasswordValidator.Validate(Password).IsValid;
    }
}

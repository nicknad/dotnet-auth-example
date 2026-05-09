using Auth.Api.Common;
using Auth.Api.Common.InputValidation;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Features.Users.Register;

/// <summary>
/// Represents a request to register a new user.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Minimal API")]
#pragma warning disable CA1515 // Consider making public types internal
public sealed record RegisterUserRequest(
#pragma warning restore CA1515 // Consider making public types internal
    [Required][MaxLength(256)][EmailAddress] string Email,
    [Required][StrongPassword] string Password,
    [Required][property: Compare("Password")] string ConfirmPassword,
    [MinLength(1)][MaxLength(100)] string FirstName,
    [MinLength(1)][MaxLength(100)] string LastName,
    IReadOnlyList<string>? Roles = null) {
    public string Email { get; init; } = Email.Trim();
}

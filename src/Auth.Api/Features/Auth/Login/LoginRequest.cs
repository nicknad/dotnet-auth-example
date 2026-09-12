using Auth.Api.Common;
using Auth.Api.Common.InputValidation;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Features.Auth.Login;

/// <summary>
/// Represents a request to log in.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Minimal API")]
#pragma warning disable CA1515 // Consider making public types internal
public sealed record LoginRequest(
#pragma warning restore CA1515 // Consider making public types internal
    [Required][MinLength(1)][MaxLength(256)][EmailAddress] string Email,
    [Required][MinLength(1)][MaxLength(128)][NotOnlyWhitespace] string Password) {
    public string Email { get; init; } = Email?.Trim() ?? string.Empty;
}

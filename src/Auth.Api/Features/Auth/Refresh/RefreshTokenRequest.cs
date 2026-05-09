using Auth.Api.Common.InputValidation;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Features.Auth.Refresh;

/// <summary>
/// Represents a request to refresh the access token.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Minimal API")]
#pragma warning disable CA1515 // Consider making public types internal
public sealed record RefreshTokenRequest(
#pragma warning restore CA1515 // Consider making public types internal
    [Required][MaxLength(500)][NotOnlyWhitespace] string RefreshToken);

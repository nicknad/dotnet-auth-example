using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Features.Users.Patch;

/// <summary>
/// Request model for updating user data.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal
public sealed record PatchUserRequest(
#pragma warning restore CA1515 // Consider making public types internal
#nullable enable
    [MinLength(1)][MaxLength(100)] string? FirstName,
    [MinLength(1)][MaxLength(100)] string? LastName,
    [MinLength(8)][MaxLength(128)] string? Password) {

    public bool IsEmpty() => FirstName is null && LastName is null && Password is null;
}
#nullable disable

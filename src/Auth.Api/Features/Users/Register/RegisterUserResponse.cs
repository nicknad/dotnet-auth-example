namespace Auth.Api.Features.Users.Register;

/// <summary>
/// Represents a response after a user is registered.
/// </summary>
internal sealed record RegisterUserResponse(string Id, string Email);

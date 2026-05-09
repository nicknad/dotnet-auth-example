namespace Auth.Api.Features.Users.GetById;

/// <summary>
/// Represents a user's details in the response.
/// </summary>
internal sealed record GetUserResponse(string Id, string Email, string FirstName, string LastName);

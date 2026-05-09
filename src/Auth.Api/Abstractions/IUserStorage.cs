using Auth.Api.Common;

namespace Auth.Api.Abstractions;

/// <summary>
/// Provides methods for user storage and management operations.
/// </summary>
internal interface IUserStorage
{
    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> FindByIdAsync(string userId);

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> FindByEmailAsync(string email);

    /// <summary>
    /// Finds a user by their refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> FindByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Adds a role to the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="role">The role to add.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthApiResult> AddRole(string userId, string role);

    /// <summary>
    /// Removes a role from the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="role">The role to remove.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthApiResult> RemoveRole(string userId, string role);

    /// <summary>
    /// Creates a new user with the specified password and roles.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="roles">The roles to assign to the user.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthApiResult> CreateAsync(ApplicationUser user, string password, List<string> roles);

    /// <summary>
    /// Checks if the provided password is valid for the specified user.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>True if the password is valid; otherwise, false.</returns>
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    /// <summary>
    /// Updates the specified user's information.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthApiResult> UpdateAsync(ApplicationUser user);

    /// <summary>
    /// Updates the password
    /// </summary>
    /// <param name="user"></param>
    /// <param name="newPassword"></param>
    /// <returns>The result of the operation</returns>
    Task<AuthApiResult> UpdatePasswordAsync(ApplicationUser user, string newPassword);

    /// <summary>
    /// Deletes the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>The result of the operation.</returns>
    Task<AuthApiResult> DeleteAsync(string userId);

    /// <summary>
    /// Gets the roles assigned to the specified user.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<IList<string>> GetRolesByUserAsync(ApplicationUser user);

    /// <summary>
    /// Lists users based on the provided query parameters.
    /// </summary>
    /// <param name="parameters">The query parameters for filtering and pagination.</param>
    /// <returns>A list of users matching the criteria.</returns>
    Task<List<ApplicationUser>> ListAsync(UserQueryParameters parameters);
}


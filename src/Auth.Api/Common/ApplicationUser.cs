using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Common;

/// <summary>
/// Represents a user in the application.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection/Identity")]
#pragma warning disable CA1515 // Consider making public types internal
public sealed class ApplicationUser : IdentityUser
#pragma warning restore CA1515 // Consider making public types internal
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the token version for invalidation.
    /// </summary>
    public int TokenVersion { get; set; } = default(int);

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token expiry time.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }
}

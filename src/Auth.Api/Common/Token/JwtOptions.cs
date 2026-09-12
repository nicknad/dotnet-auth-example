namespace Auth.Api.Common.Token;

/// <summary>
/// JWT settings resolved once at startup so token creation and validation always use the same values.
/// </summary>
internal sealed class JwtOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Insecure fallback key used only in Development and Testing when no key is configured.
    /// </summary>
    public const string DevelopmentKey = "dev-temporary-insecure-key-please-configure-this-in-production";

    /// <summary>
    /// Gets or sets the symmetric signing key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string? Audience { get; set; }
}

using Auth.Api.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Services;

/// <summary>
/// Service to manage IP blocking.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection")]
internal sealed class IpBlockingService
{
    private const string BlockKeyPrefix = "BlockedIp:";
    private readonly ICacheService cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpBlockingService"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    public IpBlockingService(ICacheService cacheService) {
        this.cacheService = cacheService;
    }

    public void BlockIp(string ipAddress, TimeSpan duration) {
        var key = $"{BlockKeyPrefix}{ipAddress}";
        this.cacheService.Store(key, true, duration);
    }

    public bool IsBlocked(string ipAddress) {
        var key = $"{BlockKeyPrefix}{ipAddress}";
        return this.cacheService.Retrieve<bool?>(key) ?? false;
    }
}

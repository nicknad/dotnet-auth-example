using Auth.Api.Infrastructure.Services;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Middleware;

/// <summary>
/// Middleware to check for blocked IPs.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection")]
internal sealed class IpBlockingMiddleware
{
    private readonly RequestDelegate next;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpBlockingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware.</param>
    public IpBlockingMiddleware(RequestDelegate next) {
        this.next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="ipBlockingService">The IP blocking service.</param>
    /// <returns>A task.</returns>
    public async Task InvokeAsync(HttpContext context, IpBlockingService ipBlockingService) {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(remoteIp) && ipBlockingService.IsBlocked(remoteIp)) {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: This IP address is blocked.").ConfigureAwait(false);
            return;
        }

        await this.next(context).ConfigureAwait(false);
    }
}

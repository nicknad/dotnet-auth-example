using Auth.Api.Features.Auth;
using Auth.Api.Features.Health;
using Auth.Api.Features.Users;
using Auth.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Extensions;

[SuppressMessage("Design", "CA1515:Consider making public types internal")]
internal static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the application middleware pipeline.
    /// </summary>
    internal static WebApplication UseApplicationPipeline(this WebApplication app, bool useRateLimiting = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                if (exception is BadHttpRequestException badRequestEx) {
                    context.Response.StatusCode = badRequestEx.StatusCode;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new {
                        error = "Invalid request"
                    });

                    return;
                }

                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new {
                    error = "Internal Server Error"
                });
            });
        });
        app.UseStatusCodePages();

        // Trust X-Forwarded-* only from loopback (default) plus explicitly configured
        // proxies. This prevents spoofing RemoteIpAddress to bypass per-IP rate
        // limiting / IP blocking or exhaust limiter partitions.
        // Configure via ForwardedHeaders:KnownProxies="10.0.0.1,172.18.0.1" and
        // ForwardedHeaders:KnownNetworks="10.0.0.0/8,172.16.0.0/12".
        var forwardedOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
        };
        foreach (var proxy in app.Configuration.GetValue<string>("ForwardedHeaders:KnownProxies")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []) {
            if (System.Net.IPAddress.TryParse(proxy, out var ip)) {
                forwardedOptions.KnownProxies.Add(ip);
            }
        }
        foreach (var network in app.Configuration.GetValue<string>("ForwardedHeaders:KnownNetworks")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []) {
            if (System.Net.IPNetwork.TryParse(network, out var ipNetwork)) {
                forwardedOptions.KnownIPNetworks.Add(ipNetwork);
            }
        }
        app.UseForwardedHeaders(forwardedOptions);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseSecurityHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseMiddleware<IpBlockingMiddleware>();
        app.UseCors("DefaultCorsPolicy");

        if (useRateLimiting)
        {
            app.UseRateLimiter();
        }

        app.UseAuthentication();
        app.UseTokenValidation();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Maps all API routes and endpoints.
    /// </summary>
    internal static WebApplication MapApplicationRoutes(this WebApplication app, bool requireRateLimiting = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthEndpoints();

        var v1 = app.MapGroup("/api/v1");
        v1.MapAuth();
        v1.MapUsers();

        if (requireRateLimiting)
        {
            v1.RequireRateLimiting("FixedWindowPolicy");
        }

        return app;
    }
}

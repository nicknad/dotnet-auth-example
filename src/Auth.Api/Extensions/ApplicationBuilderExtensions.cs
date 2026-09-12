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

        // Use forwarded headers so RemoteIpAddress and scheme are correct when behind proxies
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

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

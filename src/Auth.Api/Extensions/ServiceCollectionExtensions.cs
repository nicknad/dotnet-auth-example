using Auth.Api.Abstractions;
using Auth.Api.Common;
using Auth.Api.Common.Token;
using Auth.Api.Infrastructure.Services;
using Auth.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Auth.Api.Extensions;

/// <summary>
/// Extension methods for dependency injection service registration.
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal")]
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Kestrel server limits.
    /// </summary>
    internal static WebApplicationBuilder ConfigureKestrelLimits(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1024 * 1024;        // 1MB
            options.Limits.MaxRequestHeaderCount = 64;              // header abuse protection
        });
        return builder;
    }

    /// <summary>
    /// Adds database context to the service collection.
    /// </summary>
    internal static IServiceCollection AddApplicationDatabase(this IServiceCollection services, WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Environment.IsEnvironment("Testing"))
        {
            var dbName = $"TestDb_{Guid.NewGuid():N}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }
        else
        {
            var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(defaultConn))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(defaultConn).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        }

        return services;
    }

    /// <summary>
    /// Adds database context for testing scenarios with an in-memory database.
    /// </summary>
    internal static IServiceCollection AddTestDatabase(this IServiceCollection services, string? dbName = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        dbName ??= $"TestDb_{Guid.NewGuid():N}";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        return services;
    }

    /// <summary>
    /// Configures Identity with strong password requirements.
    /// </summary>
    internal static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Adds core application services (caching, storage, validation, etc.).
    /// </summary>
    internal static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidation();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddMemoryCache();
        services.AddAuthorization();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AllowTrailingCommas = false;
        });

        // Infrastructure services
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<IUserStorage, IdentityUserStorage>();
        services.AddScoped<ITokenHandler, Auth.Api.Infrastructure.Services.TokenHandler>();
        services.AddSingleton<IpBlockingService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Configures JWT authentication with settings from configuration.
    /// </summary>
    internal static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var allowInsecureDefaults = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        var jwtSettings = configuration.GetSection(JwtOptions.SectionName);

        var keyString = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(keyString))
        {
            if (!allowInsecureDefaults)
            {
                throw new InvalidOperationException("Jwt:Key must be configured in non-development environments.");
            }

            Log.Warning("Jwt:Key is not configured. Falling back to an insecure development key. Do not use this in production.");
            keyString = JwtOptions.DevelopmentKey;
        }

        if (Encoding.UTF8.GetByteCount(keyString) < 32 && !allowInsecureDefaults)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long for production environments.");
        }

        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if ((string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)) && !allowInsecureDefaults)
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured in non-development environments.");
        }

        services.Configure<JwtOptions>(options =>
        {
            options.Key = keyString;
            options.Issuer = issuer;
            options.Audience = audience;
        });

        var key = Encoding.UTF8.GetBytes(keyString);
        var validateIssuer = !string.IsNullOrWhiteSpace(issuer);
        var validateAudience = !string.IsNullOrWhiteSpace(audience);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = validateIssuer,
                ValidateAudience = validateAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        });

        return services;
    }

    /// <summary>
    /// Configures CORS policy from configuration or defaults to development settings.
    /// </summary>
    internal static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var corsAllowed = configuration.GetValue<string>("Cors:AllowedOrigins");
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing") && string.IsNullOrWhiteSpace(corsAllowed))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must be configured in non-development environments.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                if (!string.IsNullOrWhiteSpace(corsAllowed))
                {
                    var origins = corsAllowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
                else
                {
                    // Development fallback - permissive for local testing
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Adds security headers and policies.
    /// </summary>
    internal static IServiceCollection AddApplicationSecurityHeaders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // low effort security headers hardening- consider more comprehensive policies and CSP in production
        services.AddSecurityHeaderPolicies()
            .SetDefaultPolicy(p => p.AddDefaultApiSecurityHeaders());

        return services;
    }

    /// <summary>
    /// Configures rate limiting with standard policy.
    /// </summary>
    internal static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, int? permitLimit = null, int? windowMinutes = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        permitLimit ??= Auth.Api.Common.Constants.RateLimiting.PermitLimit;
        windowMinutes ??= Auth.Api.Common.Constants.RateLimiting.Window;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("FixedWindowPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit.Value,
                    Window = TimeSpan.FromMinutes(windowMinutes.Value),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }

    /// <summary>
    /// Adds auth logger service.
    /// </summary>
    internal static IServiceCollection AddAuthLogger(this IServiceCollection services, Logger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (logger is null)
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/auth-log.txt", rollingInterval: RollingInterval.Day)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Auth.Api")
                .CreateLogger();
        }

        services.AddSingleton<IAuthLogger>(new AuthLogger(logger));
        return services;
    }
}

using Auth.Api.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Storage;

/// <summary>
/// The database context for the application.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Dependency Injection")]
#pragma warning disable CA1515 // Consider making public types internal
internal sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
#pragma warning restore CA1515 // Consider making public types internal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) {
    }

    /// <summary>
    /// Configures the model.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
    }
}

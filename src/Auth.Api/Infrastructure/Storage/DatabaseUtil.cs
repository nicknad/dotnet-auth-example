using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Infrastructure.Storage;

internal static class DatabaseUtil
{
    public static async Task SeedDatabase(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = [Common.Constants.Roles.Admin, Common.Constants.Roles.User];

        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role)) {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }


        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) {
            logger.LogWarning("Seed:AdminEmail and Seed:AdminPassword are not configured; skipping admin account seeding.");
        } else {
            if (string.Equals(adminPassword, "Admin123!", StringComparison.Ordinal)) {
                logger.LogWarning("Seed admin is using the default development password. Change Seed:AdminPassword immediately.");
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null) {
                var adminUser = new ApplicationUser {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded) {
                    throw new InvalidOperationException(
                        $"Admin creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(adminUser, Common.Constants.Roles.Admin);
            }
        }

        // Bulk demo-user seeding removed: the previous implementation inserted 100
        // passwordless rows directly via DbContext (no PasswordHash/SecurityStamp,
        // bypassing UserManager validation) which squatted user1..100@example.com
        // and produced login-incapable accounts. Create demo data explicitly via
        // the API or a controlled seeder that uses UserManager with random passwords.
    }
}

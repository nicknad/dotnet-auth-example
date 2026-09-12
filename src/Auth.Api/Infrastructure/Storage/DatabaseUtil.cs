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


        const int userCount = 100;

        var existingEmails = await db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(u => u.Email!)
            .ToHashSetAsync();

        var newUsers = new List<ApplicationUser>(userCount);

        for (int i = 1; i <= userCount; i++) {
            var email = $"user{i}@example.com";

            if (existingEmails.Contains(email))
                continue;

            newUsers.Add(new ApplicationUser {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = $"User{i}",
                LastName = "Seeded"
            });
        }

        await db.Users.AddRangeAsync(newUsers);
        await db.SaveChangesAsync();

        var userRole = await db.Roles
            .AsNoTracking()
            .Where(r => r.Name == Common.Constants.Roles.User)
            .Select(r => r.Id)
            .SingleAsync();

        var userIds = await db.Users
            .AsNoTracking()
            .Where(u => u.Email!.StartsWith("user"))
            .Select(u => u.Id)
            .ToListAsync();

        var existingUserRolePairs = await db.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId) && ur.RoleId == userRole)
            .Select(ur => ur.UserId)
            .ToHashSetAsync();

        var userRoles = userIds
            .Where(id => !existingUserRolePairs.Contains(id))
            .Select(id => new IdentityUserRole<string> {
                UserId = id,
                RoleId = userRole
            })
            .ToList();

        if (userRoles.Count > 0) {
            await db.UserRoles.AddRangeAsync(userRoles);
            await db.SaveChangesAsync();
        }
    }
}

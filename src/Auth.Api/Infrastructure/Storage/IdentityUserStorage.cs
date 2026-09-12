using Auth.Api.Abstractions;
using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Infrastructure.Storage;

// Implementation of User Storage using Microsoft Identity framework.
// This class can be expanded in the future to include additional methods for user management if needed.
// ApplicationUser is still used as the user entity, but we can easily switch to a different implementation.
internal class IdentityUserStorage(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext) : IUserStorage
{
    public async Task<AuthApiResult> AddRole(string userId, string role) {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null) {
            return AuthApiResult.Failed("User not found");
        }

        if (!await roleManager.RoleExistsAsync(role)) {
            return AuthApiResult.Failed($"Role '{role}' does not exist");
        }

        var result = await userManager.AddToRoleAsync(user, role);

        return result.Succeeded ? AuthApiResult.Success : AuthApiResult.Failed(result.Errors.Select(e => e.Description));
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password) {
        // SignInManager applies Identity's lockout policy on failed attempts.
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public async Task<AuthApiResult> CreateAsync(ApplicationUser user, string password, List<string> roles) {
        if (roles is null || roles.Count == 0) {
            return AuthApiResult.Failed("At least one role must be assigned to the user");
        }

        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role)) {
                return AuthApiResult.Failed($"Role '{role}' does not exist");
            }
        }

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded) {
            return AuthApiResult.Failed(result.Errors.Select(e => e.Description));
        }

        // Assign roles
        foreach (var role in roles) {
            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded) {
                return AuthApiResult.Failed(roleResult.Errors.Select(e => e.Description));
            }
        }

        return AuthApiResult.Success;
    }

    public Task<ApplicationUser?> FindByEmailAsync(string email) {
        var normalizedEmail = userManager.NormalizeEmail(email);
        return userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
    }
    public Task<ApplicationUser?> FindByIdAsync(string userId) => userManager.FindByIdAsync(userId);
    public Task<ApplicationUser?> FindByRefreshTokenAsync(string refreshToken) => userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    public async Task<AuthApiResult> DeleteAsync(string userId) {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) {
            return AuthApiResult.Failed("User not found");
        }

        user.IsDeleted = true;
        user.TokenVersion++;
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        return await UpdateAsync(user);
    }
    public async Task<List<ApplicationUser>> ListAsync(UserQueryParameters parameters) {
        IQueryable<ApplicationUser> query = userManager.Users;

        if (!string.IsNullOrEmpty(parameters.Name)) {
            query = query.Where(u => (u.FirstName + " " + u.LastName).Contains(parameters.Name));
        }

        if (!string.IsNullOrEmpty(parameters.Email)) {
            query = query.Where(u => u.Email != null && u.Email.Contains(parameters.Email));
        }

        if (!string.IsNullOrEmpty(parameters.Role)) {
            query = query.Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == parameters.Role)));
        }

        // Cap page size
        var take = Math.Clamp(parameters.PageSize ?? Common.Constants.Pagination.DefaultPageSize, 1, Common.Constants.Pagination.MaxPageSize);
        var skip = Math.Max(parameters.Page ?? 0, 0) * take;

        return await query
            .OrderBy(u => u.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<AuthApiResult> UpdateAsync(ApplicationUser user) {
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? AuthApiResult.Success : AuthApiResult.Failed(result.Errors.Select(e => e.Description));
    }

    public async Task<AuthApiResult> RemoveRole(string userId, string role) {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null) {
            return AuthApiResult.Failed("User not found");
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        return result.Succeeded ? AuthApiResult.Success : AuthApiResult.Failed(result.Errors.Select(e => e.Description));
    }

    public Task<IList<string>> GetRolesByUserAsync(ApplicationUser user) {
        return userManager.GetRolesAsync(user);
    }

    public async Task<AuthApiResult> UpdatePasswordAsync(ApplicationUser user, string newPassword) {
        user.TokenVersion++;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!resetResult.Succeeded) {
            return AuthApiResult.Failed(resetResult.Errors.Select(e => e.Description));
        }

        return AuthApiResult.Success;
    }
}

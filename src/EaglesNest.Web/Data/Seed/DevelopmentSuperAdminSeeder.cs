using EaglesNest.Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EaglesNest.Web.Data.Seed;

public static class DevelopmentSuperAdminSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<DevelopmentSuperAdminOptions> options)
    {
        var seedOptions = options.Value;
        if (string.IsNullOrWhiteSpace(seedOptions.UserName) ||
            string.IsNullOrWhiteSpace(seedOptions.Email) ||
            string.IsNullOrWhiteSpace(seedOptions.Password))
        {
            return;
        }

        var userName = seedOptions.UserName.Trim();
        var email = seedOptions.Email.Trim();
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, seedOptions.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Development super-admin account could not be created: {errors}");
            }
        }
        else
        {
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.NormalizedEmail = userManager.NormalizeEmail(email);
            }

            if (user.IsLoginDisabled)
            {
                user.IsLoginDisabled = false;
                user.LoginDisabledReason = null;
            }

            await userManager.UpdateAsync(user);
        }

        var hasSystemAdminAssignment = await dbContext.RoleAssignments
            .AnyAsync(assignment => assignment.ApplicationUserId == user.Id && assignment.Position == OfficerPosition.SystemAdmin);

        if (!hasSystemAdminAssignment)
        {
            dbContext.RoleAssignments.Add(new RoleAssignment
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                OrganizationUnitId = null,
                Position = OfficerPosition.SystemAdmin
            });

            await dbContext.SaveChangesAsync();
        }
    }
}

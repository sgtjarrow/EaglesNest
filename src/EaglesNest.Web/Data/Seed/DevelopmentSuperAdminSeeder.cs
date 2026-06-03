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

        var national = await dbContext.OrganizationUnits
            .SingleOrDefaultAsync(unit => unit.Level == OrganizationLevel.National);

        if (national is null)
        {
            throw new InvalidOperationException("Development super-admin member could not be created because National was not found.");
        }

        var member = await dbContext.Members
            .SingleOrDefaultAsync(member => member.ApplicationUserId == user.Id);

        if (member is null)
        {
            member = new Member
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                FirstName = "Super",
                LastName = "Admin",
                RoadName = "super_eagle",
                IsSystemAdmin = true,
                Status = MemberStatus.PatchHolder,
                PrimaryChapterId = national.Id
            };

            dbContext.Members.Add(member);
            dbContext.MemberChapterAssignments.Add(new MemberChapterAssignment
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                ChapterId = national.Id,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                IsPrimary = true
            });
            dbContext.MemberStatusHistory.Add(new MemberStatusHistory
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                Status = member.Status,
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
                Notes = "Development super-admin bootstrap.",
                ActorName = "DevelopmentSuperAdminSeeder",
                ActorSource = "DevelopmentSeeder"
            });
        }

        member.IsSystemAdmin = true;

        await dbContext.SaveChangesAsync();
    }
}

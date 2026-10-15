using System.Security.Cryptography;
using System.Text;
using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Common;
using EaglesNest.Application.Security;
using EaglesNest.Domain.Accounts;
using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Infrastructure.Services;

public sealed class UserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuditEventWriter audit,
    IApplicationEmailSender emailSender) : IUserAdministrationService
{
    public async Task<IReadOnlyList<UserAccountSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.OrderBy(x => x.UserName).ToListAsync(cancellationToken);
        var results = new List<UserAccountSummary>(users.Count);
        foreach (var user in users)
        {
            results.Add(new UserAccountSummary(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.ApprovalStatus.ToString(),
                user.IsManuallyLocked || user.LockoutEnd > DateTimeOffset.UtcNow,
                user.IsDeleted,
                user.MustChangePassword,
                (await userManager.GetRolesAsync(user)).ToArray()));
        }

        return results;
    }

    public async Task<OperationResult> CreateAsync(string userName, string email, string setPasswordBaseUri, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByNameAsync(userName) is not null || await userManager.FindByEmailAsync(email) is not null)
        {
            return OperationResult.Failure("That username or email address is already reserved.");
        }

        var user = new ApplicationUser
        {
            UserName = userName.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            ApprovalStatus = AccountApprovalStatus.Approved,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            ApprovedByUserId = actorUserId,
            MustChangePassword = true,
            LockoutEnabled = true
        };

        var temporarySecret = $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))}aA1!";
        var strategy = dbContext.Database.CreateExecutionStrategy();
        var url = string.Empty;
        var outcome = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var create = await userManager.CreateAsync(user, temporarySecret);
            if (!create.Succeeded)
            {
                return OperationResult.Failure(string.Join(" ", create.Errors.Select(x => x.Description)));
            }

            await userManager.AddToRoleAsync(user, SystemRoles.User);
            url = await BuildResetUrlAsync(user, setPasswordBaseUri);
            await audit.WriteAsync(actorUserId, actorName, AuditActions.UserCreated,
                new { TargetUserId = user.Id, user.UserName, user.Email, Source = "Admin" }, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });

        if (outcome.Succeeded) await emailSender.SendSetPasswordAsync(user.Email!, url, cancellationToken);
        return outcome;
    }

    public async Task<OperationResult> SetManualLockAsync(string userId, bool locked, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            return OperationResult.Failure("You cannot lock your own account.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return OperationResult.Failure("The account was not found.");
        }

        if (locked && await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("The final active Admin cannot be locked.");
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            user.IsManuallyLocked = locked;
            user.LockoutEnd = locked ? DateTimeOffset.MaxValue : null;
            if (!locked) await userManager.ResetAccessFailedCountAsync(user);

            await userManager.UpdateSecurityStampAsync(user);
            await audit.WriteAsync(actorUserId, actorName, locked ? AuditActions.UserLocked : AuditActions.UserUnlocked,
                new { TargetUserId = user.Id, user.UserName }, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });
    }

    public async Task<OperationResult> SoftDeleteAsync(string userId, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            return OperationResult.Failure("You cannot delete your own account.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return OperationResult.Failure("The account was not found.");
        }

        if (await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("The final active Admin cannot be deleted.");
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            user.IsDeleted = true;
            user.DeletedAtUtc = DateTimeOffset.UtcNow;
            user.DeletedByUserId = actorUserId;
            user.IsManuallyLocked = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await userManager.UpdateSecurityStampAsync(user);
            await audit.WriteAsync(actorUserId, actorName, AuditActions.UserDeleted,
                new { TargetUserId = user.Id, user.UserName, user.Email }, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });
    }

    public async Task<OperationResult> IssuePasswordResetAsync(string userId, string resetBaseUri, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted || user.ApprovalStatus != AccountApprovalStatus.Approved)
        {
            return OperationResult.Failure("Only active, approved accounts can receive password resets.");
        }

        var url = await BuildResetUrlAsync(user, resetBaseUri);
        await emailSender.SendPasswordResetAsync(user.Email!, url, cancellationToken);
        await audit.WriteAsync(actorUserId, actorName, AuditActions.PasswordResetIssued,
            new { TargetUserId = user.Id, user.UserName }, cancellationToken: cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetRolesAsync(string userId, IReadOnlyCollection<string> roles, string actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var allowedRoles = new[] { SystemRoles.User, SystemRoles.Admin };
        var desired = roles.Intersect(allowedRoles, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        desired.Add(SystemRoles.User);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return OperationResult.Failure("The account was not found.");
        }

        var current = await userManager.GetRolesAsync(user);
        if (current.Contains(SystemRoles.Admin) && !desired.Contains(SystemRoles.Admin) && await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("The Admin role cannot be removed from the final active Admin.");
        }

        foreach (var role in desired)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                return OperationResult.Failure($"Role '{role}' does not exist.");
            }
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var remove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToArray();
            var add = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();
            if (remove.Length > 0) await userManager.RemoveFromRolesAsync(user, remove);
            if (add.Length > 0) await userManager.AddToRolesAsync(user, add);
            await userManager.UpdateSecurityStampAsync(user);
            await audit.WriteAsync(actorUserId, actorName, AuditActions.RolesChanged,
                new { TargetUserId = user.Id, user.UserName, Roles = desired.Order() }, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });
    }

    private async Task<bool> IsLastActiveAdminAsync(ApplicationUser user)
    {
        if (!await userManager.IsInRoleAsync(user, SystemRoles.Admin)) return false;
        var admins = await userManager.GetUsersInRoleAsync(SystemRoles.Admin);
        return admins.Count(x => !x.IsDeleted && !x.IsManuallyLocked && x.ApprovalStatus == AccountApprovalStatus.Approved) <= 1;
    }

    private async Task<string> BuildResetUrlAsync(ApplicationUser user, string baseUri)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return QueryHelpers.AddQueryString(baseUri, new Dictionary<string, string?>
        {
            ["userId"] = user.Id,
            ["code"] = code
        });
    }
}

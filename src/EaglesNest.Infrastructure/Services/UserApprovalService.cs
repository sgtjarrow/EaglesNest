using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Common;
using EaglesNest.Application.Security;
using EaglesNest.Domain.Accounts;
using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Infrastructure.Services;

public sealed class UserApprovalService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditEventWriter audit,
    IApplicationEmailSender emailSender) : IUserApprovalService
{
    public async Task<OperationResult> ApproveAsync(string userId, string actorUserId, string actorName, string? notes, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return OperationResult.Failure("The account was not found.");
        }

        if (user.ApprovalStatus == AccountApprovalStatus.Approved)
        {
            return OperationResult.Success();
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var outcome = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            user.ApprovalStatus = AccountApprovalStatus.Approved;
            user.ApprovedAtUtc = DateTimeOffset.UtcNow;
            user.ApprovedByUserId = actorUserId;
            user.RejectedAtUtc = null;
            user.RejectedByUserId = null;
            user.EmailConfirmed = true;

            var update = await userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                return OperationResult.Failure(string.Join(" ", update.Errors.Select(x => x.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, SystemRoles.User))
            {
                await userManager.AddToRoleAsync(user, SystemRoles.User);
            }

            await audit.WriteAsync(actorUserId, actorName, AuditActions.UserApproved,
                new { TargetUserId = user.Id, user.UserName, user.Email }, notes, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });

        if (outcome.Succeeded) await emailSender.SendApprovalAsync(user.Email!, cancellationToken);
        return outcome;
    }

    public async Task<OperationResult> RejectAsync(string userId, string actorUserId, string actorName, string? notes, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
        {
            return OperationResult.Failure("The account was not found.");
        }

        if (user.ApprovalStatus == AccountApprovalStatus.Approved)
        {
            return OperationResult.Failure("An approved account cannot be rejected. Lock or delete it instead.");
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            user.ApprovalStatus = AccountApprovalStatus.Rejected;
            user.RejectedAtUtc = DateTimeOffset.UtcNow;
            user.RejectedByUserId = actorUserId;
            await userManager.UpdateSecurityStampAsync(user);
            await audit.WriteAsync(actorUserId, actorName, AuditActions.UserRejected,
                new { TargetUserId = user.Id, user.UserName, user.Email }, notes, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        });
    }
}

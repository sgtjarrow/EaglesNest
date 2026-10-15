using EaglesNest.Domain.Accounts;
using Microsoft.AspNetCore.Identity;

namespace EaglesNest.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public AccountApprovalStatus ApprovalStatus { get; set; } = AccountApprovalStatus.Pending;
    public string? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? RejectedByUserId { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public bool IsManuallyLocked { get; set; }
    public bool IsDeleted { get; set; }
    public string? DeletedByUserId { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool MustChangePassword { get; set; }
}

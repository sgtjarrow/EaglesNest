using EaglesNest.Application.Common;

namespace EaglesNest.Application.Abstractions;

public interface IUserApprovalService
{
    Task<OperationResult> ApproveAsync(string userId, string actorUserId, string actorName, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> RejectAsync(string userId, string actorUserId, string actorName, string? notes, CancellationToken cancellationToken = default);
}

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserAccountSummary>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> CreateAsync(string userName, string email, string setPasswordBaseUri, string actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<OperationResult> SetManualLockAsync(string userId, bool locked, string actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<OperationResult> SoftDeleteAsync(string userId, string actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<OperationResult> IssuePasswordResetAsync(string userId, string resetBaseUri, string actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<OperationResult> SetRolesAsync(string userId, IReadOnlyCollection<string> roles, string actorUserId, string actorName, CancellationToken cancellationToken = default);
}

public interface IPermissionAuthorizationService
{
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
}

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEventSummary>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}

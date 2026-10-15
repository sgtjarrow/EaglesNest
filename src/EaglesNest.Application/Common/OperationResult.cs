namespace EaglesNest.Application.Common;

public sealed record OperationResult(bool Succeeded, string? Error = null)
{
    public static OperationResult Success() => new(true);
    public static OperationResult Failure(string error) => new(false, error);
}

public sealed record UserAccountSummary(
    string Id,
    string UserName,
    string Email,
    string ApprovalStatus,
    bool IsLocked,
    bool IsDeleted,
    bool MustChangePassword,
    IReadOnlyList<string> Roles);

public sealed record AuditEventSummary(
    Guid Id,
    string ActorName,
    string Action,
    DateTimeOffset OccurredAtUtc,
    string DetailsJson,
    string? Notes);

public sealed record DevelopmentEmailSummary(
    Guid Id,
    string Recipient,
    string Subject,
    string ActionUrl,
    DateTimeOffset CreatedAtUtc);

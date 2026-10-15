using EaglesNest.Application.Common;

namespace EaglesNest.Application.Abstractions;

public interface IAuditEventWriter
{
    Task WriteAsync(string? actorUserId, string actorName, string action, object? details = null, string? notes = null, CancellationToken cancellationToken = default);
}

public interface IApplicationEmailSender
{
    Task SendApprovalAsync(string recipient, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default);
    Task SendSetPasswordAsync(string recipient, string setPasswordUrl, CancellationToken cancellationToken = default);
}

public interface IDevelopmentEmailOutbox
{
    IReadOnlyList<DevelopmentEmailSummary> GetMessages();
}

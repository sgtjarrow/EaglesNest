using System.Collections.Concurrent;
using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Common;
using Microsoft.Extensions.Logging;

namespace EaglesNest.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IApplicationEmailSender, IDevelopmentEmailOutbox
{
    private readonly ConcurrentQueue<DevelopmentEmailSummary> _messages = new();

    public Task SendApprovalAsync(string recipient, CancellationToken cancellationToken = default) =>
        CaptureAsync(recipient, "Eagles Nest access approved", string.Empty);

    public Task SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default) =>
        CaptureAsync(recipient, "Reset your Eagles Nest password", resetUrl);

    public Task SendSetPasswordAsync(string recipient, string setPasswordUrl, CancellationToken cancellationToken = default) =>
        CaptureAsync(recipient, "Set your Eagles Nest password", setPasswordUrl);

    public IReadOnlyList<DevelopmentEmailSummary> GetMessages() =>
        _messages.OrderByDescending(x => x.CreatedAtUtc).Take(50).ToArray();

    private Task CaptureAsync(string recipient, string subject, string actionUrl)
    {
        _messages.Enqueue(new DevelopmentEmailSummary(Guid.NewGuid(), recipient, subject, actionUrl, DateTimeOffset.UtcNow));
        logger.LogInformation("Development email captured for {Recipient}: {Subject}. The tokenized URL is available only in the protected development outbox.", recipient, subject);
        return Task.CompletedTask;
    }
}

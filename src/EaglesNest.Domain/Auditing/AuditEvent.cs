namespace EaglesNest.Domain.Auditing;

public sealed class AuditEvent
{
    private AuditEvent() { }

    public AuditEvent(
        string? actorUserId,
        string actorName,
        string action,
        string detailsJson,
        string? notes = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        ActorName = actorName;
        Action = action;
        DetailsJson = detailsJson;
        Notes = notes;
        OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string? ActorUserId { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string DetailsJson { get; private set; } = "{}";
    public string? Notes { get; private set; }
}

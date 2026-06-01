using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Chapters;

public sealed record ChapterAuditLogItem
{
    public long Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public AuditAction Action { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string ActorName { get; init; } = string.Empty;
    public string ActorSource { get; init; } = string.Empty;
}

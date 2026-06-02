using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public sealed record MemberStatusHistoryItem
{
    public Guid Id { get; init; }
    public MemberStatus Status { get; init; }
    public DateOnly EffectiveDate { get; init; }
    public string? Notes { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public string ActorSource { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

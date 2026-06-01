using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public sealed record MemberListItem
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string? RoadName { get; init; }
    public string ChapterName { get; init; } = string.Empty;
    public string ChapterAbbreviation { get; init; } = string.Empty;
    public MemberStatus Status { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

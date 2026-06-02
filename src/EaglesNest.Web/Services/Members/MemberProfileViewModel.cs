using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public sealed record MemberProfileViewModel
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string? RoadName { get; init; }
    public string? PreferredName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? BloodType { get; init; }
    public string? Gender { get; init; }
    public DateOnly? LifetimeDate { get; init; }
    public MemberStatus Status { get; init; }
    public string ChapterName { get; init; } = string.Empty;
    public string ChapterAbbreviation { get; init; } = string.Empty;
    public IReadOnlyList<MilitaryServiceEditModel> MilitaryServiceRecords { get; init; } = [];
}

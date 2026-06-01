using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public sealed class MemberEditModel
{
    public Guid? Id { get; set; }
    public string? ApplicationUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public string? PreferredName { get; set; }
    public string? RoadName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Prospect;
    public Guid? PrimaryChapterId { get; set; }
    public DateOnly? JoinedOn { get; set; }
    public string? Notes { get; set; }
    public DateOnly ChapterEffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public List<MilitaryServiceEditModel> MilitaryServiceRecords { get; set; } = [];
}

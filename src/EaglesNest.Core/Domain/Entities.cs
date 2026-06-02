namespace EaglesNest.Core.Domain;

public class OrganizationUnit
{
    public Guid Id { get; set; }
    public Guid? ParentOrganizationUnitId { get; set; }
    public OrganizationUnit? ParentOrganizationUnit { get; set; }
    public ICollection<OrganizationUnit> Children { get; set; } = [];
    public OrganizationLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public DateOnly? CharterDate { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? MailingAddressLine1 { get; set; }
    public string? MailingAddressLine2 { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingStateCode { get; set; }
    public string? MailingPostalCode { get; set; }
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Operating;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ChapterSuspension
{
    public Guid Id { get; set; }
    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorSource { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class StateChapterAssignment
{
    public Guid Id { get; set; }
    public Guid StateOrganizationUnitId { get; set; }
    public OrganizationUnit StateOrganizationUnit { get; set; } = null!;
    public Guid LocalChapterOrganizationUnitId { get; set; }
    public OrganizationUnit LocalChapterOrganizationUnit { get; set; } = null!;
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorSource { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Member
{
    public Guid Id { get; set; }
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
    public string? BloodType { get; set; }
    public string? Gender { get; set; }
    public DateOnly? LifetimeDate { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Prospect;
    public Guid PrimaryChapterId { get; set; }
    public OrganizationUnit PrimaryChapter { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<MemberChapterAssignment> ChapterAssignments { get; set; } = [];
    public ICollection<MemberStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<MilitaryServiceRecord> MilitaryServiceRecords { get; set; } = [];
}

public class MemberChapterAssignment
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public Guid ChapterId { get; set; }
    public OrganizationUnit Chapter { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsPrimary { get; set; } = true;
}

public class MilitaryServiceRecord
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string Branch { get; set; } = string.Empty;
    public string? Rank { get; set; }
    public DateOnly? ServiceStartDate { get; set; }
    public DateOnly? ServiceEndDate { get; set; }
    public string? DischargeType { get; set; }
    public string? ConflictTab { get; set; }
    public string? ServiceNotes { get; set; }
}

public class MemberStatusHistory
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public MemberStatus Status { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorSource { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MemberChangeRequest
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
    public string RequestedChangesJson { get; set; } = "{}";
    public string? ReviewedByUserId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class RideEvent
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public OrganizationUnit Chapter { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public decimal? PlannedMiles { get; set; }
    public string? Notes { get; set; }
    public ICollection<RideAttendance> Attendance { get; set; } = [];
}

public class RideAttendance
{
    public Guid Id { get; set; }
    public Guid RideEventId { get; set; }
    public RideEvent RideEvent { get; set; } = null!;
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public RideAttendanceStatus Status { get; set; } = RideAttendanceStatus.Planned;
    public decimal? ActualMiles { get; set; }
    public string? Notes { get; set; }
}

public class FinancialAssessment
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public Guid ChapterId { get; set; }
    public OrganizationUnit Chapter { get; set; } = null!;
    public FinancialRecordType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly AssessedOn { get; set; }
    public DateOnly? DueOn { get; set; }
    public string? Notes { get; set; }
    public ICollection<FinancialPayment> Payments { get; set; } = [];
}

public class FinancialPayment
{
    public Guid Id { get; set; }
    public Guid FinancialAssessmentId { get; set; }
    public FinancialAssessment FinancialAssessment { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class RoleAssignment
{
    public Guid Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
    public OfficerPosition Position { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public string? ApplicationUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorSource { get; set; } = string.Empty;
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ImportBatch
{
    public Guid Id { get; set; }
    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CommittedAt { get; set; }
    public ICollection<ImportBatchRow> Rows { get; set; } = [];
}

public class ImportBatchRow
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;
    public int RowNumber { get; set; }
    public string RawJson { get; set; } = "{}";
    public string? ValidationMessage { get; set; }
    public bool IsValid { get; set; }
}

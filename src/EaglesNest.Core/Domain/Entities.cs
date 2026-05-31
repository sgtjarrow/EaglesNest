namespace EaglesNest.Core.Domain;

public class OrganizationUnit
{
    public Guid Id { get; set; }
    public Guid? ParentOrganizationUnitId { get; set; }
    public OrganizationUnit? ParentOrganizationUnit { get; set; }
    public ICollection<OrganizationUnit> Children { get; set; } = [];
    public OrganizationLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CharterNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Member
{
    public Guid Id { get; set; }
    public string? ApplicationUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Prospect;
    public Guid PrimaryChapterId { get; set; }
    public OrganizationUnit PrimaryChapter { get; set; } = null!;
    public DateOnly? JoinedOn { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<MemberChapterAssignment> ChapterAssignments { get; set; } = [];
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
    public string? ServiceNotes { get; set; }
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

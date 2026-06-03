namespace EaglesNest.Core.Domain;

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Approved = 4,
    Rejected = 5,
    LoginDisabled = 6,
    LoginEnabled = 7,
    Imported = 8,
    Closed = 9,
    Reopened = 10,
    Suspended = 11,
    SuspensionEnded = 12,
    StateChapterAssigned = 13,
    MemberAdded = 14,
    MemberRemoved = 15,
    MemberUpdated = 16,
    MemberChapterTransferred = 17,
    MilitaryServiceAdded = 18,
    MilitaryServiceUpdated = 19,
    MilitaryServiceRemoved = 20,
    MemberStatusChanged = 21,
    RoleAssignmentCreated = 22,
    RoleAssignmentRemoved = 23,
    RoleAssignmentExpired = 24,
    SystemAdminGranted = 25,
    SystemAdminRemoved = 26
}

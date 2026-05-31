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
    Imported = 8
}

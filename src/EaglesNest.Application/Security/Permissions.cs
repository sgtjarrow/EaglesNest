namespace EaglesNest.Application.Security;

public static class Permissions
{
    public const string ViewDashboard = "Dashboard.View";
    public const string ManageUsers = "Users.Manage";
    public const string ManageRoles = "Roles.Manage";
    public const string ViewAuditLog = "Audit.View";

    public static readonly IReadOnlyList<string> All =
    [
        ViewDashboard,
        ManageUsers,
        ManageRoles,
        ViewAuditLog
    ];
}

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class AuditActions
{
    public const string AccessRequested = "Account.AccessRequested";
    public const string UserCreated = "Account.UserCreated";
    public const string UserApproved = "Account.UserApproved";
    public const string UserRejected = "Account.UserRejected";
    public const string UserLocked = "Account.UserLocked";
    public const string UserUnlocked = "Account.UserUnlocked";
    public const string UserDeleted = "Account.UserDeleted";
    public const string PasswordResetIssued = "Account.PasswordResetIssued";
    public const string PasswordResetCompleted = "Account.PasswordResetCompleted";
    public const string PasswordChanged = "Account.PasswordChanged";
    public const string LoginSucceeded = "Authentication.LoginSucceeded";
    public const string LoginFailed = "Authentication.LoginFailed";
    public const string Logout = "Authentication.Logout";
    public const string RolesChanged = "Security.RolesChanged";
    public const string BootstrapAdminCreated = "System.BootstrapAdminCreated";
}

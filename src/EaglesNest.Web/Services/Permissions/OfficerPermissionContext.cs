using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Permissions;

public sealed class OfficerPermissionContext
{
    public bool IsAuthenticated { get; init; }
    public bool IsSystemAdmin { get; init; }
    public Guid? MemberId { get; init; }
    public string? MemberDisplayName { get; init; }
    public bool CanAccessAdmin => IsSystemAdmin || CanViewMembers || CanEditAnyChapter || CanManageRoles;
    public bool CanViewMembers { get; init; }
    public bool CanEditMembers { get; init; }
    public bool CanEditAnyChapter { get; init; }
    public bool CanManageRoles { get; init; }
    public bool CanViewAllMembers { get; init; }
    public bool CanEditAllMembers { get; init; }
    public bool CanEditAllChapters { get; init; }
    public bool CanManageAllRoles { get; init; }
    public IReadOnlySet<Guid> ViewMemberChapterIds { get; init; } = new HashSet<Guid>();
    public IReadOnlySet<Guid> EditMemberChapterIds { get; init; } = new HashSet<Guid>();
    public IReadOnlySet<Guid> EditChapterIds { get; init; } = new HashSet<Guid>();
    public IReadOnlySet<Guid> ManageRoleChapterIds { get; init; } = new HashSet<Guid>();
    public IReadOnlyList<OfficerRoleScope> Roles { get; init; } = [];

    public bool CanViewMemberInChapter(Guid chapterId)
    {
        return CanViewAllMembers || ViewMemberChapterIds.Contains(chapterId) || CanEditMemberInChapter(chapterId);
    }

    public bool CanEditMemberInChapter(Guid chapterId)
    {
        return IsSystemAdmin || CanEditAllMembers || EditMemberChapterIds.Contains(chapterId);
    }

    public bool CanEditChapter(Guid chapterId)
    {
        return IsSystemAdmin || CanEditAllChapters || EditChapterIds.Contains(chapterId);
    }

    public bool CanManageRolesInChapter(Guid chapterId)
    {
        return IsSystemAdmin || CanManageAllRoles || ManageRoleChapterIds.Contains(chapterId);
    }
}

public sealed record OfficerRoleScope(
    Guid RoleAssignmentId,
    OfficerPosition Position,
    Guid? OrganizationUnitId,
    string OrganizationName,
    string OrganizationAbbreviation,
    OrganizationLevel OrganizationLevel);

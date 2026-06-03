using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services.Permissions;

public class OfficerPermissionService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    private static readonly OfficerPosition[] EditPositions =
    [
        OfficerPosition.President,
        OfficerPosition.VicePresident
    ];

    public async Task<OfficerPermissionContext> GetPermissionsAsync(string? applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            return new OfficerPermissionContext();
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var member = await dbContext.Members
            .AsNoTracking()
            .Include(existing => existing.PrimaryChapter)
            .SingleOrDefaultAsync(existing => existing.ApplicationUserId == applicationUserId);

        if (member is null)
        {
            return new OfficerPermissionContext { IsAuthenticated = true };
        }

        var now = DateTimeOffset.UtcNow;
        if (member.IsSystemAdmin)
        {
            return new OfficerPermissionContext
            {
                IsAuthenticated = true,
                IsSystemAdmin = true,
                MemberId = member.Id,
                MemberDisplayName = DisplayMember(member),
                CanViewMembers = true,
                CanEditMembers = true,
                CanEditAnyChapter = true,
                CanManageRoles = true,
                CanViewAllMembers = true,
                CanEditAllMembers = true,
                CanEditAllChapters = true,
                CanManageAllRoles = true,
                Roles = []
            };
        }

        var roleAssignments = await dbContext.RoleAssignments
            .AsNoTracking()
            .Include(assignment => assignment.OrganizationUnit)
            .Where(assignment => assignment.MemberId == member.Id &&
                                 (assignment.ExpiresAt == null || assignment.ExpiresAt > now))
            .ToListAsync();

        var roleScopes = roleAssignments
            .Select(assignment => new OfficerRoleScope(
                assignment.Id,
                assignment.Position,
                assignment.OrganizationUnitId,
                assignment.OrganizationUnit.Name,
                assignment.OrganizationUnit.Abbreviation,
                assignment.OrganizationUnit.Level))
            .ToList();

        var viewMemberChapterIds = new HashSet<Guid>();
        var editMemberChapterIds = new HashSet<Guid>();
        var editChapterIds = new HashSet<Guid>();
        var manageRoleChapterIds = new HashSet<Guid>();
        var canViewAllMembers = false;
        var canEditAllMembers = false;
        var canEditAllChapters = false;
        var canManageAllRoles = false;

        var chapterIdsByState = await GetLocalChapterIdsByStateAsync(dbContext);
        var actingStateChapterIds = await dbContext.StateChapterAssignments
            .AsNoTracking()
            .Where(assignment => assignment.EndsOn == null)
            .Select(assignment => assignment.LocalChapterOrganizationUnitId)
            .ToListAsync();
        var actingStateChapterIdSet = actingStateChapterIds.ToHashSet();

        foreach (var assignment in roleAssignments)
        {
            var scope = assignment.OrganizationUnit ?? member.PrimaryChapter;
            if (scope.Level == OrganizationLevel.National)
            {
                if (EditPositions.Contains(assignment.Position))
                {
                    canViewAllMembers = true;
                    canEditAllMembers = true;
                    canEditAllChapters = true;
                    canManageAllRoles = true;
                }
                else if (assignment.Position == OfficerPosition.Secretary)
                {
                    canViewAllMembers = true;
                }

                continue;
            }

            if (scope.Level != OrganizationLevel.LocalChapter)
            {
                continue;
            }

            if (EditPositions.Contains(assignment.Position))
            {
                viewMemberChapterIds.Add(scope.Id);
                editMemberChapterIds.Add(scope.Id);
                editChapterIds.Add(scope.Id);
                manageRoleChapterIds.Add(scope.Id);

                if (actingStateChapterIdSet.Contains(scope.Id) &&
                    scope.ParentOrganizationUnitId is not null &&
                    chapterIdsByState.TryGetValue(scope.ParentOrganizationUnitId.Value, out var childChapterIds))
                {
                    foreach (var childChapterId in childChapterIds)
                    {
                        viewMemberChapterIds.Add(childChapterId);
                        manageRoleChapterIds.Add(childChapterId);
                    }
                }
            }
            else if (assignment.Position == OfficerPosition.Secretary)
            {
                viewMemberChapterIds.Add(scope.Id);
                if (actingStateChapterIdSet.Contains(scope.Id) &&
                    scope.ParentOrganizationUnitId is not null &&
                    chapterIdsByState.TryGetValue(scope.ParentOrganizationUnitId.Value, out var childChapterIds))
                {
                    foreach (var childChapterId in childChapterIds)
                    {
                        viewMemberChapterIds.Add(childChapterId);
                    }
                }
            }
        }

        return new OfficerPermissionContext
        {
            IsAuthenticated = true,
            MemberId = member.Id,
            MemberDisplayName = DisplayMember(member),
            CanViewMembers = canViewAllMembers || viewMemberChapterIds.Count > 0 || editMemberChapterIds.Count > 0,
            CanEditMembers = canEditAllMembers || editMemberChapterIds.Count > 0,
            CanEditAnyChapter = canEditAllChapters || editChapterIds.Count > 0,
            CanManageRoles = canManageAllRoles || manageRoleChapterIds.Count > 0,
            CanViewAllMembers = canViewAllMembers,
            CanEditAllMembers = canEditAllMembers,
            CanEditAllChapters = canEditAllChapters,
            CanManageAllRoles = canManageAllRoles,
            ViewMemberChapterIds = viewMemberChapterIds,
            EditMemberChapterIds = editMemberChapterIds,
            EditChapterIds = editChapterIds,
            ManageRoleChapterIds = manageRoleChapterIds,
            Roles = roleScopes
        };
    }

    private static async Task<Dictionary<Guid, List<Guid>>> GetLocalChapterIdsByStateAsync(ApplicationDbContext dbContext)
    {
        var chapters = await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unit.Level == OrganizationLevel.LocalChapter &&
                           unit.ParentOrganizationUnitId != null &&
                           unit.Status != OrganizationStatus.Closed)
            .Select(unit => new
            {
                StateId = unit.ParentOrganizationUnitId!.Value,
                unit.Id
            })
            .ToListAsync();

        return chapters
            .GroupBy(chapter => chapter.StateId)
            .ToDictionary(group => group.Key, group => group.Select(chapter => chapter.Id).ToList());
    }

    private static string DisplayMember(Member member)
    {
        if (!string.IsNullOrWhiteSpace(member.RoadName))
        {
            return member.RoadName;
        }

        if (!string.IsNullOrWhiteSpace(member.PreferredName))
        {
            return member.PreferredName;
        }

        return $"{member.FirstName} {member.LastName}".Trim();
    }
}

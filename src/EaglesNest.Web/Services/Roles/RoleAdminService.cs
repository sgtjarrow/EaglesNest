using System.Text.Json;
using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Chapters;
using EaglesNest.Web.Services.Members;
using EaglesNest.Web.Services.Permissions;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services.Roles;

public class RoleAdminService(ApplicationDbContext dbContext, ChapterAdminService chapterService)
{
    private const int MaxSystemAdmins = 2;

    public static readonly OfficerPosition[] LocalAssignablePositions =
    [
        OfficerPosition.President,
        OfficerPosition.VicePresident,
        OfficerPosition.Treasurer,
        OfficerPosition.Secretary,
        OfficerPosition.SergeantAtArms,
        OfficerPosition.RoadCaptain
    ];

    public static readonly OfficerPosition[] AssignablePositions =
    [
        OfficerPosition.President,
        OfficerPosition.VicePresident,
        OfficerPosition.Treasurer,
        OfficerPosition.Secretary,
        OfficerPosition.SergeantAtArms,
        OfficerPosition.RoadCaptain,
        OfficerPosition.MasterSergeantAtArms,
        OfficerPosition.CyberIntel
    ];

    public RoleAdminService(ApplicationDbContext dbContext)
        : this(dbContext, new ChapterAdminService(dbContext))
    {
    }

    public async Task<IReadOnlyList<RoleMemberListItem>> SearchMembersAsync(string? search, OfficerPermissionContext permissions)
    {
        if (!permissions.CanManageRoles)
        {
            return [];
        }

        var query = dbContext.Members
            .AsNoTracking()
            .Include(member => member.PrimaryChapter)
            .AsQueryable();

        if (!permissions.CanManageAllRoles)
        {
            query = query.Where(member => permissions.ManageRoleChapterIds.Contains(member.PrimaryChapterId));
        }

        var text = search?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(member =>
                member.FirstName.Contains(text) ||
                member.LastName.Contains(text) ||
                (member.PreferredName != null && member.PreferredName.Contains(text)) ||
                (member.RoadName != null && member.RoadName.Contains(text)) ||
                member.PrimaryChapter.Name.Contains(text) ||
                member.PrimaryChapter.Abbreviation.Contains(text));
        }

        var members = await query
            .OrderBy(member => member.PrimaryChapter.Level == OrganizationLevel.National ? 0 : 1)
            .ThenBy(member => member.PrimaryChapter.Abbreviation)
            .ThenBy(member => member.RoadName ?? member.LastName)
            .ThenBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .Take(250)
            .ToListAsync();

        return members.Select(member => new RoleMemberListItem(
            member.Id,
            DisplayName(member),
            LegalName(member),
            member.PrimaryChapter.Name,
            member.PrimaryChapter.Abbreviation,
            member.Status))
            .ToList();
    }

    public async Task<IReadOnlyList<RoleAssignmentItem>> GetAssignmentsAsync(Guid memberId)
    {
        return await dbContext.RoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.MemberId == memberId)
            .OrderBy(assignment => assignment.Position)
            .Select(assignment => new RoleAssignmentItem(
                assignment.Id,
                assignment.MemberId,
                assignment.Position,
                assignment.OrganizationUnitId,
                assignment.OrganizationUnit.Name,
                assignment.OrganizationUnit.Abbreviation,
                assignment.AssignedAt,
                assignment.ExpiresAt))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RoleChapterOption>> GetManageableChapterOptionsAsync(OfficerPermissionContext permissions)
    {
        if (!permissions.CanManageRoles)
        {
            return [];
        }

        var chapters = await chapterService.GetChapterOptionsAsync(
            permissions.ManageRoleChapterIds,
            permissions.CanManageAllRoles);

        return chapters
            .Select(chapter => new RoleChapterOption(
                chapter.Id,
                chapter.Name,
                chapter.Abbreviation,
                chapter.Level,
                chapter.StateName,
                chapter.StateAbbreviation))
            .ToList();
    }

    public async Task<IReadOnlyList<RoleMemberListItem>> GetSystemAdminsAsync()
    {
        var members = await dbContext.Members
            .AsNoTracking()
            .Include(member => member.PrimaryChapter)
            .Where(member => member.IsSystemAdmin)
            .OrderBy(member => member.RoadName ?? member.LastName)
            .ThenBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync();

        return members.Select(member => new RoleMemberListItem(
            member.Id,
            DisplayName(member),
            LegalName(member),
            member.PrimaryChapter.Name,
            member.PrimaryChapter.Abbreviation,
            member.Status))
            .ToList();
    }

    public async Task<RoleSaveResult> AddAssignmentAsync(
        Guid memberId,
        OfficerPosition position,
        Guid organizationUnitId,
        OfficerPermissionContext permissions,
        MemberActor actor)
    {
        if (!permissions.CanManageRolesInChapter(organizationUnitId))
        {
            return RoleSaveResult.Failure("You do not have permission to assign roles for that chapter.");
        }

        if (!GetAssignablePositions(permissions, organizationUnitId).Contains(position))
        {
            return RoleSaveResult.Failure("That officer position cannot be assigned here.");
        }

        var member = await dbContext.Members
            .Include(existing => existing.PrimaryChapter)
            .SingleOrDefaultAsync(existing => existing.Id == memberId);
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == organizationUnitId);

        if (member is null || organization is null)
        {
            return RoleSaveResult.Failure("Member or chapter was not found.");
        }

        if (organization.Level == OrganizationLevel.State)
        {
            return RoleSaveResult.Failure("Officer roles must be assigned to National or a local chapter.");
        }

        if (member.PrimaryChapterId != organizationUnitId)
        {
            return RoleSaveResult.Failure("Role scope must match the member's primary chapter.");
        }

        var exists = await dbContext.RoleAssignments.AnyAsync(assignment =>
            assignment.MemberId == memberId &&
            assignment.OrganizationUnitId == organizationUnitId &&
            assignment.Position == position);
        if (exists)
        {
            return RoleSaveResult.Failure("That role assignment already exists.");
        }

        var assignment = new RoleAssignment
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            OrganizationUnitId = organizationUnitId,
            Position = position
        };
        dbContext.RoleAssignments.Add(assignment);
        AddAudit(AuditAction.RoleAssignmentCreated, member, organization, position, actor);

        await dbContext.SaveChangesAsync();
        return RoleSaveResult.Success();
    }

    public async Task<RoleSaveResult> GrantSystemAdminAsync(Guid memberId, OfficerPermissionContext permissions, MemberActor actor)
    {
        if (!permissions.IsSystemAdmin)
        {
            return RoleSaveResult.Failure("Only SystemAdmin can grant SystemAdmin.");
        }

        var member = await dbContext.Members
            .Include(existing => existing.PrimaryChapter)
            .SingleOrDefaultAsync(existing => existing.Id == memberId);
        if (member is null)
        {
            return RoleSaveResult.Failure("Member was not found.");
        }

        if (member.IsSystemAdmin)
        {
            return RoleSaveResult.Failure("Member is already a SystemAdmin.");
        }

        var count = await dbContext.Members.CountAsync(existing => existing.IsSystemAdmin);
        if (count >= MaxSystemAdmins)
        {
            return RoleSaveResult.Failure($"Only {MaxSystemAdmins} SystemAdmin members are allowed.");
        }

        member.IsSystemAdmin = true;
        AddSystemAdminAudit(AuditAction.SystemAdminGranted, member, actor);
        await dbContext.SaveChangesAsync();
        return RoleSaveResult.Success();
    }

    public async Task<RoleSaveResult> RemoveSystemAdminAsync(Guid memberId, OfficerPermissionContext permissions, MemberActor actor)
    {
        if (!permissions.IsSystemAdmin)
        {
            return RoleSaveResult.Failure("Only SystemAdmin can remove SystemAdmin.");
        }

        var member = await dbContext.Members
            .Include(existing => existing.PrimaryChapter)
            .SingleOrDefaultAsync(existing => existing.Id == memberId);
        if (member is null)
        {
            return RoleSaveResult.Failure("Member was not found.");
        }

        if (!member.IsSystemAdmin)
        {
            return RoleSaveResult.Failure("Member is not a SystemAdmin.");
        }

        var count = await dbContext.Members.CountAsync(existing => existing.IsSystemAdmin);
        if (count <= 1 && permissions.MemberId == memberId)
        {
            return RoleSaveResult.Failure("Cannot remove the last SystemAdmin from yourself.");
        }

        member.IsSystemAdmin = false;
        AddSystemAdminAudit(AuditAction.SystemAdminRemoved, member, actor);
        await dbContext.SaveChangesAsync();
        return RoleSaveResult.Success();
    }

    public IReadOnlyList<OfficerPosition> GetAssignablePositions(OfficerPermissionContext permissions, Guid organizationUnitId)
    {
        if (permissions.IsSystemAdmin || permissions.CanManageAllRoles)
        {
            return AssignablePositions;
        }

        return LocalAssignablePositions;
    }

    public async Task<RoleSaveResult> RemoveAssignmentAsync(Guid assignmentId, OfficerPermissionContext permissions, MemberActor actor)
    {
        var assignment = await dbContext.RoleAssignments
            .Include(existing => existing.Member)
            .Include(existing => existing.OrganizationUnit)
            .SingleOrDefaultAsync(existing => existing.Id == assignmentId);

        if (assignment is null || assignment.OrganizationUnit is null)
        {
            return RoleSaveResult.Failure("Role assignment was not found.");
        }

        if (!permissions.CanManageRolesInChapter(assignment.OrganizationUnit.Id))
        {
            return RoleSaveResult.Failure("You do not have permission to remove roles for that chapter.");
        }

        dbContext.RoleAssignments.Remove(assignment);
        AddAudit(AuditAction.RoleAssignmentRemoved, assignment.Member, assignment.OrganizationUnit, assignment.Position, actor);

        await dbContext.SaveChangesAsync();
        return RoleSaveResult.Success();
    }

    public static string DisplayPosition(OfficerPosition position)
    {
        return position switch
        {
            OfficerPosition.VicePresident => "Vice President",
            OfficerPosition.SergeantAtArms => "Sergeant At Arms",
            OfficerPosition.RoadCaptain => "Road Captain",
            OfficerPosition.MasterSergeantAtArms => "Master Sergeant At Arms",
            OfficerPosition.CyberIntel => "Cyber Intel",
            _ => position.ToString()
        };
    }

    private void AddAudit(AuditAction action, Member member, OrganizationUnit organization, OfficerPosition position, MemberActor actor)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource,
            OrganizationUnitId = organization.Id,
            Action = action,
            EntityName = nameof(Member),
            EntityId = member.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new
            {
                MemberId = member.Id,
                Member = DisplayName(member),
                Position = DisplayPosition(position),
                Chapter = organization.Abbreviation,
                Action = action.ToString()
            })
        });
    }

    private void AddSystemAdminAudit(AuditAction action, Member member, MemberActor actor)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource,
            OrganizationUnitId = member.PrimaryChapterId,
            Action = action,
            EntityName = nameof(Member),
            EntityId = member.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new
            {
                MemberId = member.Id,
                Member = DisplayName(member),
                Action = action.ToString()
            })
        });
    }

    private static string DisplayName(Member member)
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

    private static string LegalName(Member member)
    {
        var parts = new[] { member.FirstName, member.MiddleName, member.LastName, member.Suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(' ', parts);
    }
}

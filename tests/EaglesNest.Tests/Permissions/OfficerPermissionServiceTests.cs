using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Members;
using EaglesNest.Web.Services.Permissions;
using EaglesNest.Web.Services.Roles;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Tests.Permissions;

public class OfficerPermissionServiceTests
{
    private static readonly MemberActor TestActor = new("admin-login", "admin@example.com", "User");

    [Fact]
    public async Task LoginWithoutLinkedMember_HasNoOfficerPermissions()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var service = new OfficerPermissionService(database);

        var permissions = await service.GetPermissionsAsync("missing-login");

        Assert.True(permissions.IsAuthenticated);
        Assert.False(permissions.CanAccessAdmin);
        Assert.False(permissions.CanViewMembers);
    }

    [Fact]
    public async Task SystemAdminMember_HasFullAccess()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var member = AddMember(dbContext, "super-login", "Super", "Admin", national.Id);
        AddRole(dbContext, member.Id, national.Id, OfficerPosition.SystemAdmin);
        await dbContext.SaveChangesAsync();
        var service = new OfficerPermissionService(database);

        var permissions = await service.GetPermissionsAsync("super-login");

        Assert.True(permissions.IsSystemAdmin);
        Assert.True(permissions.CanEditAllMembers);
        Assert.True(permissions.CanEditAllChapters);
        Assert.True(permissions.CanManageAllRoles);
    }

    [Fact]
    public async Task ActingStateSecretary_CanViewChildChapterMembersButCannotEdit()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var (national, state, actingChapter, childChapter) = AddStateSetup(dbContext);
        var secretary = AddMember(dbContext, "sec-login", "State", "Secretary", actingChapter.Id);
        AddRole(dbContext, secretary.Id, actingChapter.Id, OfficerPosition.Secretary);
        dbContext.StateChapterAssignments.Add(new StateChapterAssignment
        {
            Id = Guid.NewGuid(),
            StateOrganizationUnitId = state.Id,
            LocalChapterOrganizationUnitId = actingChapter.Id,
            StartsOn = new DateOnly(2026, 1, 1),
            ActorName = "test",
            ActorSource = "Test"
        });
        await dbContext.SaveChangesAsync();
        var service = new OfficerPermissionService(database);

        var permissions = await service.GetPermissionsAsync("sec-login");

        Assert.True(permissions.CanViewMemberInChapter(actingChapter.Id));
        Assert.True(permissions.CanViewMemberInChapter(childChapter.Id));
        Assert.False(permissions.CanEditMemberInChapter(childChapter.Id));
        Assert.False(permissions.CanManageRolesInChapter(childChapter.Id));
        Assert.False(permissions.CanEditChapter(childChapter.Id));
    }

    [Fact]
    public async Task AddAssignmentAsync_WritesMemberAuditEntry()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var member = AddMember(dbContext, "target-login", "Target", "Member", national.Id);
        var adminMember = AddMember(dbContext, "admin-login", "Admin", "Member", national.Id);
        AddRole(dbContext, adminMember.Id, national.Id, OfficerPosition.SystemAdmin);
        await dbContext.SaveChangesAsync();

        var permissions = await new OfficerPermissionService(database).GetPermissionsAsync("admin-login");
        var service = new RoleAdminService(dbContext);

        var result = await service.AddAssignmentAsync(member.Id, OfficerPosition.President, national.Id, permissions, TestActor);

        Assert.True(result.Succeeded);
        Assert.True(await dbContext.RoleAssignments.AnyAsync(role =>
            role.MemberId == member.Id &&
            role.OrganizationUnitId == national.Id &&
            role.Position == OfficerPosition.President));
        Assert.True(await dbContext.AuditLogs.AnyAsync(log =>
            log.EntityName == nameof(Member) &&
            log.EntityId == member.Id.ToString() &&
            log.Action == AuditAction.RoleAssignmentCreated));
    }

    private static TestDbContextFactory CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static (OrganizationUnit National, OrganizationUnit State, OrganizationUnit ActingChapter, OrganizationUnit ChildChapter) AddStateSetup(ApplicationDbContext dbContext)
    {
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var actingChapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        var childChapter = AddOrganization(dbContext, "Jensen Beach", "FLA-4", OrganizationLevel.LocalChapter, state.Id);
        return (national, state, actingChapter, childChapter);
    }

    private static OrganizationUnit AddOrganization(
        ApplicationDbContext dbContext,
        string name,
        string abbreviation,
        OrganizationLevel level,
        Guid? parentId)
    {
        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = abbreviation,
            Level = level,
            ParentOrganizationUnitId = parentId,
            Status = OrganizationStatus.Open
        };

        dbContext.OrganizationUnits.Add(organization);
        return organization;
    }

    private static Member AddMember(ApplicationDbContext dbContext, string loginId, string firstName, string lastName, Guid primaryChapterId)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = loginId,
            FirstName = firstName,
            LastName = lastName,
            RoadName = firstName,
            Status = MemberStatus.PatchHolder,
            PrimaryChapterId = primaryChapterId
        };

        dbContext.Members.Add(member);
        return member;
    }

    private static RoleAssignment AddRole(ApplicationDbContext dbContext, Guid memberId, Guid chapterId, OfficerPosition position)
    {
        var role = new RoleAssignment
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            OrganizationUnitId = chapterId,
            Position = position
        };

        dbContext.RoleAssignments.Add(role);
        return role;
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(options);
        }
    }
}

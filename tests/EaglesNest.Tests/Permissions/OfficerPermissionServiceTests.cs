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
        member.IsSystemAdmin = true;
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
        adminMember.IsSystemAdmin = true;
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

    [Fact]
    public async Task RemoveAssignmentAsync_ExpiresRoleAndKeepsHistory()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var member = AddMember(dbContext, "target-login", "Target", "Member", national.Id);
        var adminMember = AddMember(dbContext, "admin-login", "Admin", "Member", national.Id);
        adminMember.IsSystemAdmin = true;
        var assignment = AddRole(dbContext, member.Id, national.Id, OfficerPosition.President);
        await dbContext.SaveChangesAsync();

        var permissions = await new OfficerPermissionService(database).GetPermissionsAsync("admin-login");
        var service = new RoleAdminService(dbContext);

        var result = await service.RemoveAssignmentAsync(assignment.Id, permissions, TestActor);

        Assert.True(result.Succeeded);
        var saved = await dbContext.RoleAssignments.SingleAsync(role => role.Id == assignment.Id);
        Assert.NotNull(saved.ExpiresAt);
        Assert.Empty(await service.GetAssignmentsAsync(member.Id));
        var history = await service.GetAssignmentHistoryAsync(member.Id);
        Assert.Single(history);
        Assert.NotNull(history[0].ExpiresAt);
        Assert.Equal("admin@example.com", history[0].EndedBy);
    }

    [Fact]
    public async Task AddAssignmentAsync_AllowsReassigningExpiredRole()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var member = AddMember(dbContext, "target-login", "Target", "Member", national.Id);
        var adminMember = AddMember(dbContext, "admin-login", "Admin", "Member", national.Id);
        adminMember.IsSystemAdmin = true;
        var expired = AddRole(dbContext, member.Id, national.Id, OfficerPosition.President);
        expired.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();

        var permissions = await new OfficerPermissionService(database).GetPermissionsAsync("admin-login");
        var service = new RoleAdminService(dbContext);

        var result = await service.AddAssignmentAsync(member.Id, OfficerPosition.President, national.Id, permissions, TestActor);

        Assert.True(result.Succeeded);
        Assert.Equal(2, await dbContext.RoleAssignments.CountAsync(role =>
            role.MemberId == member.Id &&
            role.OrganizationUnitId == national.Id &&
            role.Position == OfficerPosition.President));
        Assert.Single(await service.GetAssignmentsAsync(member.Id));
    }

    [Fact]
    public async Task GetAssignablePositions_LocalRoleManager_HidesNationalSpecialRoles()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var (_, _, actingChapter, _) = AddStateSetup(dbContext);
        var president = AddMember(dbContext, "pres-login", "Local", "President", actingChapter.Id);
        AddRole(dbContext, president.Id, actingChapter.Id, OfficerPosition.President);
        await dbContext.SaveChangesAsync();

        var permissions = new OfficerPermissionContext
        {
            CanManageRoles = true,
            ManageRoleChapterIds = new HashSet<Guid> { actingChapter.Id }
        };
        var service = new RoleAdminService(dbContext);

        var positions = service.GetAssignablePositions(permissions, actingChapter.Id);

        Assert.Contains(OfficerPosition.President, positions);
        Assert.Contains(OfficerPosition.RoadCaptain, positions);
        Assert.DoesNotContain(OfficerPosition.MasterSergeantAtArms, positions);
        Assert.DoesNotContain(OfficerPosition.CyberIntel, positions);
    }

    [Fact]
    public async Task ExpiredRoleAssignment_DoesNotGrantPermissions()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var (_, _, actingChapter, _) = AddStateSetup(dbContext);
        var president = AddMember(dbContext, "pres-login", "Local", "President", actingChapter.Id);
        var role = AddRole(dbContext, president.Id, actingChapter.Id, OfficerPosition.President);
        role.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();
        var service = new OfficerPermissionService(database);

        var permissions = await service.GetPermissionsAsync("pres-login");

        Assert.False(permissions.CanViewMembers);
        Assert.False(permissions.CanEditAnyChapter);
        Assert.False(permissions.CanManageRoles);
    }

    [Fact]
    public async Task GrantSystemAdminAsync_RejectsThirdSystemAdmin()
    {
        var database = CreateDatabase();
        await using var dbContext = database.CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var currentAdmin = AddMember(dbContext, "admin-login", "Admin", "One", national.Id);
        var secondAdmin = AddMember(dbContext, "admin-2-login", "Admin", "Two", national.Id);
        var target = AddMember(dbContext, "target-login", "Target", "Member", national.Id);
        currentAdmin.IsSystemAdmin = true;
        secondAdmin.IsSystemAdmin = true;
        await dbContext.SaveChangesAsync();

        var permissions = new OfficerPermissionContext
        {
            IsSystemAdmin = true,
            MemberId = currentAdmin.Id,
            CanManageRoles = true,
            CanManageAllRoles = true
        };
        var service = new RoleAdminService(dbContext);

        var result = await service.GrantSystemAdminAsync(target.Id, permissions, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("2 SystemAdmin", StringComparison.OrdinalIgnoreCase));
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

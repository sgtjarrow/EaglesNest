using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Members;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Tests.Members;

public class MemberAdminServiceTests
{
    private static readonly MemberActor TestActor = new("user-1", "tester@example.com", "User");

    [Fact]
    public async Task CreateAsync_AllowsDuplicateLegalNames()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var first = await service.CreateAsync(NewMember("John", "Smith", "Hammer", chapter.Id), TestActor);
        var second = await service.CreateAsync(NewMember("John", "Smith", "Anvil", chapter.Id), TestActor);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(2, await dbContext.Members.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateRoadNameInSamePrimaryChapter()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        Assert.True((await service.CreateAsync(NewMember("John", "Smith", "Hammer", chapter.Id), TestActor)).Succeeded);
        var duplicate = await service.CreateAsync(NewMember("Jane", "Jones", "Hammer", chapter.Id), TestActor);

        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, error => error.Contains("Road name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_AllowsSameRoadNameInDifferentChapters()
    {
        await using var dbContext = CreateDbContext();
        var (state, chapter, _) = AddChapterSetup(dbContext);
        var secondChapter = AddOrganization(dbContext, "Jensen Beach", "FLA-4", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var first = await service.CreateAsync(NewMember("John", "Smith", "Hammer", chapter.Id), TestActor);
        var second = await service.CreateAsync(NewMember("Jane", "Jones", "Hammer", secondChapter.Id), TestActor);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_RejectsRoadNameConflictInDestinationChapter()
    {
        await using var dbContext = CreateDbContext();
        var (state, firstChapter, _) = AddChapterSetup(dbContext);
        var secondChapter = AddOrganization(dbContext, "Jensen Beach", "FLA-4", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var first = await service.CreateAsync(NewMember("John", "Smith", "Hammer", firstChapter.Id), TestActor);
        Assert.True(first.Succeeded);
        Assert.True((await service.CreateAsync(NewMember("Jane", "Jones", "Hammer", secondChapter.Id), TestActor)).Succeeded);

        var edit = (await service.GetMemberAsync(first.MemberId!.Value))!;
        edit.PrimaryChapterId = secondChapter.Id;

        var result = await service.UpdateAsync(first.MemberId.Value, edit, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Road name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAsync_TransfersPrimaryChapterWithHistory()
    {
        await using var dbContext = CreateDbContext();
        var (state, firstChapter, _) = AddChapterSetup(dbContext);
        var secondChapter = AddOrganization(dbContext, "Jensen Beach", "FLA-4", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var created = await service.CreateAsync(NewMember("John", "Smith", "Hammer", firstChapter.Id), TestActor);
        var edit = (await service.GetMemberAsync(created.MemberId!.Value))!;
        edit.PrimaryChapterId = secondChapter.Id;
        edit.ChapterEffectiveDate = new DateOnly(2026, 6, 1);

        var result = await service.UpdateAsync(created.MemberId.Value, edit, TestActor);

        Assert.True(result.Succeeded);
        var member = await dbContext.Members.SingleAsync(member => member.Id == created.MemberId);
        Assert.Equal(secondChapter.Id, member.PrimaryChapterId);

        var assignments = await dbContext.MemberChapterAssignments.OrderBy(assignment => assignment.StartDate).ToListAsync();
        Assert.Equal(new DateOnly(2026, 5, 31), assignments[0].EndDate);
        Assert.Null(assignments[1].EndDate);
        Assert.Equal(secondChapter.Id, assignments[1].ChapterId);
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.Action == AuditAction.MemberChapterTransferred));
    }

    [Fact]
    public async Task UpdateAsync_DeceasedTransfersMemberToEternalChapter()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, eternal) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var created = await service.CreateAsync(NewMember("John", "Smith", "Hammer", chapter.Id), TestActor);
        var edit = (await service.GetMemberAsync(created.MemberId!.Value))!;
        edit.Status = MemberStatus.Deceased;
        edit.ChapterEffectiveDate = new DateOnly(2026, 6, 1);

        var result = await service.UpdateAsync(created.MemberId.Value, edit, TestActor);

        Assert.True(result.Succeeded);
        var member = await dbContext.Members.SingleAsync(member => member.Id == created.MemberId);
        Assert.Equal(MemberStatus.Deceased, member.Status);
        Assert.Equal(eternal.Id, member.PrimaryChapterId);
    }

    [Fact]
    public async Task CreateAsync_StoresMultipleMilitaryServiceRecords()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);
        var input = NewMember("John", "Smith", "Hammer", chapter.Id);
        input.MilitaryServiceRecords.Add(new MilitaryServiceEditModel { Branch = "Army", Rank = "Sgt" });
        input.MilitaryServiceRecords.Add(new MilitaryServiceEditModel { Branch = "Navy", Rank = "Po2" });

        var result = await service.CreateAsync(input, TestActor);

        Assert.True(result.Succeeded);
        Assert.Equal(2, await dbContext.MilitaryServiceRecords.CountAsync(record => record.MemberId == result.MemberId));
    }

    [Fact]
    public async Task CreateAsync_WithBlankMilitaryBranch_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);
        var input = NewMember("John", "Smith", "Hammer", chapter.Id);
        input.MilitaryServiceRecords.Add(new MilitaryServiceEditModel { Branch = "" });

        var result = await service.CreateAsync(input, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("branch is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAsync_WithNullMilitaryBranch_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);
        var created = await service.CreateAsync(NewMember("John", "Smith", "Hammer", chapter.Id), TestActor);
        var input = (await service.GetMemberAsync(created.MemberId!.Value))!;
        input.MilitaryServiceRecords.Add(new MilitaryServiceEditModel { Branch = null! });

        var result = await service.UpdateAsync(created.MemberId.Value, input, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("branch is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_RejectsLoginLinkedToAnotherMember()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        dbContext.Users.Add(new ApplicationUser { Id = "login-1", UserName = "linked_user" });
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);

        var first = NewMember("John", "Smith", "Hammer", chapter.Id);
        first.ApplicationUserId = "login-1";
        Assert.True((await service.CreateAsync(first, TestActor)).Succeeded);

        var second = NewMember("Jane", "Jones", "Anvil", chapter.Id);
        second.ApplicationUserId = "login-1";
        var result = await service.CreateAsync(second, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("already assigned", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProfileForUserAsync_ReturnsLinkedReadOnlyProfile()
    {
        await using var dbContext = CreateDbContext();
        var (_, chapter, _) = AddChapterSetup(dbContext);
        dbContext.Users.Add(new ApplicationUser { Id = "login-1", UserName = "linked_user" });
        await dbContext.SaveChangesAsync();
        var service = new MemberAdminService(dbContext);
        var input = NewMember("John", "Smith", "Hammer", chapter.Id);
        input.ApplicationUserId = "login-1";
        Assert.True((await service.CreateAsync(input, TestActor)).Succeeded);

        var profile = await service.GetProfileForUserAsync("login-1");
        var label = await service.GetMemberProfileLabelAsync("login-1");

        Assert.NotNull(profile);
        Assert.Equal("Hammer", profile.DisplayName);
        Assert.Equal("Hammer", label);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (OrganizationUnit State, OrganizationUnit Chapter, OrganizationUnit Eternal) AddChapterSetup(ApplicationDbContext dbContext)
    {
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        var eternal = AddOrganization(dbContext, "Eternal Chapter", "Chapter-100", OrganizationLevel.LocalChapter, national.Id);
        return (state, chapter, eternal);
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
            Status = OrganizationStatus.Operating
        };

        dbContext.OrganizationUnits.Add(organization);
        return organization;
    }

    private static MemberEditModel NewMember(string firstName, string lastName, string roadName, Guid chapterId)
    {
        return new MemberEditModel
        {
            FirstName = firstName,
            LastName = lastName,
            RoadName = roadName,
            Status = MemberStatus.Active,
            PrimaryChapterId = chapterId,
            ChapterEffectiveDate = new DateOnly(2026, 1, 1)
        };
    }
}

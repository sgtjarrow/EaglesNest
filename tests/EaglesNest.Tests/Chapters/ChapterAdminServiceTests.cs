using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Chapters;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Tests.Chapters;

public class ChapterAdminServiceTests
{
    private static readonly ChapterActor TestActor = new("user-1", "tester@example.com", "User");

    [Fact]
    public async Task CreateAsync_RejectsDuplicateAbbreviation()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var result = await service.CreateAsync(new ChapterEditModel
        {
            Name = "Duplicate National",
            Abbreviation = "NAT",
            Level = OrganizationLevel.State,
            ParentOrganizationUnitId = national.Id
        }, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_RejectsStateUnderLocalChapter()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Georgia", "GA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Atlanta", "GA-1", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var result = await service.CreateAsync(new ChapterEditModel
        {
            Name = "Bad State",
            Abbreviation = "BAD",
            Level = OrganizationLevel.State,
            ParentOrganizationUnitId = chapter.Id
        }, TestActor);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("State parent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetHierarchyAsync_HidesClosedAndSuspendedByDefault()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        AddOrganization(dbContext, "Georgia", "GA", OrganizationLevel.State, national.Id, OrganizationStatus.Closed);
        AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id, OrganizationStatus.Operating);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var defaultHierarchy = await service.GetHierarchyAsync();
        var fullHierarchy = await service.GetHierarchyAsync(includeUnavailable: true);

        Assert.DoesNotContain(defaultHierarchy.Single().Children, child => child.Abbreviation == "GA");
        Assert.Contains(fullHierarchy.Single().Children, child => child.Abbreviation == "GA");
    }

    [Fact]
    public async Task CloseAsync_RejectsNational()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var result = await service.CloseAsync(national.Id, TestActor);

        Assert.False(result.Succeeded);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == national.Id)).Status);
    }

    [Fact]
    public async Task CloseAndSuspendAsync_RejectEternalChapter()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var eternal = AddOrganization(dbContext, "Eternal Chapter", "Chapter-100", OrganizationLevel.LocalChapter, national.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.False((await service.CloseAsync(eternal.Id, TestActor)).Succeeded);
        Assert.False((await service.SuspendAsync(eternal.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, null, TestActor)).Succeeded);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == eternal.Id)).Status);
    }

    [Fact]
    public async Task CloseAndReopenAsync_UpdatesChapterStatus()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.CloseAsync(chapter.Id, TestActor)).Succeeded);
        Assert.Equal(OrganizationStatus.Closed, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);
        Assert.Equal(OrganizationStatus.Closed, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);

        Assert.True((await service.ReopenAsync(chapter.Id, TestActor)).Succeeded);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);
    }

    [Fact]
    public async Task CloseAsync_ClosesParentStateWhenLastLocalChapterCloses()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        dbContext.StateChapterAssignments.Add(new StateChapterAssignment
        {
            Id = Guid.NewGuid(),
            StateOrganizationUnitId = state.Id,
            LocalChapterOrganizationUnitId = chapter.Id,
            StartsOn = new DateOnly(2020, 1, 1),
            ActorName = TestActor.ActorName,
            ActorSource = TestActor.ActorSource
        });
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.CloseAsync(chapter.Id, TestActor)).Succeeded);

        Assert.Equal(OrganizationStatus.Closed, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);
        Assert.Equal(OrganizationStatus.Closed, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);
        Assert.NotNull((await dbContext.StateChapterAssignments.SingleAsync()).EndsOn);
        Assert.Equal(2, await dbContext.AuditLogs.CountAsync(log => log.Action == AuditAction.Closed));
    }

    [Fact]
    public async Task GetHierarchyAsync_DoesNotShowActingChapterForClosedState()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id, OrganizationStatus.Closed);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id, OrganizationStatus.Closed);
        dbContext.StateChapterAssignments.Add(new StateChapterAssignment
        {
            Id = Guid.NewGuid(),
            StateOrganizationUnitId = state.Id,
            LocalChapterOrganizationUnitId = chapter.Id,
            StartsOn = new DateOnly(2020, 1, 1),
            ActorName = TestActor.ActorName,
            ActorSource = TestActor.ActorSource
        });
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var hierarchy = await service.GetHierarchyAsync(includeUnavailable: true);
        var closedState = hierarchy.Single().Children.Single(child => child.Id == state.Id);

        Assert.Null(closedState.ActingStateChapterId);
        Assert.Null(closedState.ActingStateChapterAbbreviation);
    }

    [Fact]
    public async Task CloseAsync_LeavesParentStateOperatingWhenSuspendedChapterRemains()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        AddOrganization(dbContext, "Jensen Beach", "FLA-4", OrganizationLevel.LocalChapter, state.Id, OrganizationStatus.Suspended);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.CloseAsync(chapter.Id, TestActor)).Succeeded);

        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);
    }

    [Fact]
    public async Task ReopenAsync_ReopensClosedParentState()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id, OrganizationStatus.Closed);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id, OrganizationStatus.Closed);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.ReopenAsync(chapter.Id, TestActor)).Succeeded);

        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);
    }

    [Fact]
    public async Task CreateAsync_UnderClosedState_ReopensStateAndAssignsNewChapter()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id, OrganizationStatus.Closed);
        var oldChapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id, OrganizationStatus.Closed);
        dbContext.StateChapterAssignments.Add(new StateChapterAssignment
        {
            Id = Guid.NewGuid(),
            StateOrganizationUnitId = state.Id,
            LocalChapterOrganizationUnitId = oldChapter.Id,
            StartsOn = new DateOnly(2020, 1, 1),
            ActorName = TestActor.ActorName,
            ActorSource = TestActor.ActorSource
        });
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var result = await service.CreateAsync(new ChapterEditModel
        {
            Name = "Jensen Beach",
            Abbreviation = "FLA-4",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = state.Id
        }, TestActor);

        Assert.True(result.Succeeded);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == state.Id)).Status);

        var currentAssignment = await dbContext.StateChapterAssignments.SingleAsync(assignment => assignment.EndsOn == null);
        Assert.Equal(result.ChapterId, currentAssignment.LocalChapterOrganizationUnitId);
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.OrganizationUnitId == state.Id && log.Action == AuditAction.Reopened));
        Assert.True(await dbContext.AuditLogs.AnyAsync(log => log.OrganizationUnitId == state.Id && log.Action == AuditAction.StateChapterAssigned));
    }

    [Fact]
    public async Task SuspendAndEndSuspensionAsync_TracksSuspensionHistory()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);
        var startsOn = new DateOnly(2026, 5, 31);
        var endsOn = new DateOnly(2026, 6, 30);

        Assert.True((await service.SuspendAsync(chapter.Id, startsOn, null, "Test suspension", TestActor)).Succeeded);
        Assert.Equal(OrganizationStatus.Suspended, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);

        Assert.True((await service.EndSuspensionAsync(chapter.Id, endsOn, TestActor)).Succeeded);
        var suspension = await dbContext.ChapterSuspensions.SingleAsync();
        Assert.Equal(endsOn, suspension.EndsOn);
        Assert.Equal(OrganizationStatus.Operating, (await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id)).Status);
    }

    [Fact]
    public async Task SuspendAsync_WithPlannedEndDate_RemainsCurrentSuspension()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);
        var startsOn = DateOnly.FromDateTime(DateTime.UtcNow);
        var plannedEndsOn = startsOn.AddDays(30);

        Assert.True((await service.SuspendAsync(chapter.Id, startsOn, plannedEndsOn, "Thirty days", TestActor)).Succeeded);

        var currentSuspension = await service.GetCurrentSuspensionAsync(chapter.Id);
        Assert.NotNull(currentSuspension);
        Assert.Equal(plannedEndsOn, currentSuspension.EndsOn);
    }

    [Fact]
    public async Task AssignStateChapterAsync_EndsPreviousCurrentAssignment()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var firstChapter = AddOrganization(dbContext, "The Originals", "FLA-1", OrganizationLevel.LocalChapter, state.Id);
        var secondChapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.AssignStateChapterAsync(state.Id, firstChapter.Id, new DateOnly(2026, 1, 1), null, TestActor)).Succeeded);
        Assert.True((await service.AssignStateChapterAsync(state.Id, secondChapter.Id, new DateOnly(2026, 5, 31), null, TestActor)).Succeeded);

        var assignments = await dbContext.StateChapterAssignments.OrderBy(assignment => assignment.StartsOn).ToListAsync();
        Assert.Equal(new DateOnly(2026, 5, 30), assignments[0].EndsOn);
        Assert.Null(assignments[1].EndsOn);
        Assert.Equal(secondChapter.Id, assignments[1].LocalChapterOrganizationUnitId);
    }

    [Fact]
    public async Task AbandonEmptyStateAsync_RemovesStateAndAudit()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);
        var result = await service.CreateAsync(new ChapterEditModel
        {
            Name = "Colorado",
            Abbreviation = "CO",
            Level = OrganizationLevel.State,
            ParentOrganizationUnitId = national.Id
        }, TestActor);

        Assert.True(result.Succeeded);
        Assert.True((await service.AbandonEmptyStateAsync(result.ChapterId!.Value)).Succeeded);
        Assert.False(await dbContext.OrganizationUnits.AnyAsync(unit => unit.Abbreviation == "CO"));
        Assert.False(await dbContext.AuditLogs.AnyAsync());
    }

    [Fact]
    public async Task AbandonEmptyStateAsync_RejectsStateWithChildChapter()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Colorado", "CO", OrganizationLevel.State, national.Id);
        AddOrganization(dbContext, "Denver", "CO-1", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);
        var result = await service.AbandonEmptyStateAsync(state.Id);

        Assert.False(result.Succeeded);
        Assert.True(await dbContext.OrganizationUnits.AnyAsync(unit => unit.Id == state.Id));
    }

    [Fact]
    public async Task AuditEntries_IncludeActorInformation()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        await service.CloseAsync(state.Id, TestActor);

        var audit = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("user-1", audit.ApplicationUserId);
        Assert.Equal("tester@example.com", audit.ActorName);
        Assert.Equal("User", audit.ActorSource);
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateChapterLocationAndCharterDate()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);
        var charterDate = new DateOnly(2020, 1, 2);

        var result = await service.UpdateAsync(chapter.Id, new ChapterEditModel
        {
            Id = chapter.Id,
            Name = "Tampa",
            Abbreviation = "FLA-7",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = state.Id,
            City = "Pinellas Park",
            StateCode = "FL",
            CharterDate = charterDate
        }, TestActor);

        Assert.True(result.Succeeded);
        var updated = await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id);
        Assert.Equal("Pinellas Park", updated.City);
        Assert.Equal("FL", updated.StateCode);
        Assert.Equal(charterDate, updated.CharterDate);
    }

    [Fact]
    public async Task UpdateAsync_NormalizesNameAndMailingAddress()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Alabama", "AL", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "IRREGULARS", "AL-4", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        var result = await service.UpdateAsync(chapter.Id, new ChapterEditModel
        {
            Id = chapter.Id,
            Name = "IRREGULARS",
            Abbreviation = "AL-4",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = state.Id,
            MailingAddressLine1 = "18615 Jefferson St.",
            MailingCity = "Athens",
            MailingStateCode = "al",
            MailingPostalCode = "35611"
        }, TestActor);

        Assert.True(result.Succeeded);
        var updated = await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id);
        Assert.Equal("Irregulars", updated.Name);
        Assert.Equal("18615 Jefferson St.", updated.MailingAddressLine1);
        Assert.Equal("Athens", updated.MailingCity);
        Assert.Equal("AL", updated.MailingStateCode);
        Assert.Equal("35611", updated.MailingPostalCode);
    }

    [Fact]
    public async Task GetChapterAuditLogsAsync_ShowsDetailAuditButExcludesOperationalLogs()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new ChapterAdminService(dbContext);

        Assert.True((await service.UpdateAsync(chapter.Id, new ChapterEditModel
        {
            Id = chapter.Id,
            Name = "Tampa Bay",
            Abbreviation = "FLA-7",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = state.Id
        }, TestActor)).Succeeded);
        Assert.True((await service.SuspendAsync(chapter.Id, new DateOnly(2026, 5, 31), null, "Test", TestActor)).Succeeded);

        var auditLogs = await service.GetChapterAuditLogsAsync(chapter.Id);

        Assert.Contains(auditLogs, log => log.Action == AuditAction.Updated && log.Summary.Contains("Name changed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(auditLogs, log => log.Action == AuditAction.Suspended);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static OrganizationUnit AddOrganization(
        ApplicationDbContext dbContext,
        string name,
        string abbreviation,
        OrganizationLevel level,
        Guid? parentId,
        OrganizationStatus status = OrganizationStatus.Operating)
    {
        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = abbreviation,
            Level = level,
            ParentOrganizationUnitId = parentId,
            Status = status
        };

        dbContext.OrganizationUnits.Add(organization);
        return organization;
    }
}

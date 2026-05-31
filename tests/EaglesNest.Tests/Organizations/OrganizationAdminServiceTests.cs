using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Organizations;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Tests.Organizations;

public class OrganizationAdminServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsDuplicateAbbreviation()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationAdminService(dbContext);

        var result = await service.CreateAsync(new OrganizationEditModel
        {
            Name = "Duplicate National",
            Abbreviation = "NAT",
            Level = OrganizationLevel.State,
            ParentOrganizationUnitId = national.Id
        }, null);

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

        var service = new OrganizationAdminService(dbContext);

        var result = await service.CreateAsync(new OrganizationEditModel
        {
            Name = "Bad State",
            Abbreviation = "BAD",
            Level = OrganizationLevel.State,
            ParentOrganizationUnitId = chapter.Id
        }, null);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("State parent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_RejectsLocalChapterUnderNational()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationAdminService(dbContext);

        var result = await service.CreateAsync(new OrganizationEditModel
        {
            Name = "Atlanta",
            Abbreviation = "GA-1",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = national.Id
        }, null);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("Local chapter parent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SetActiveAsync_RejectsNationalDeactivation()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationAdminService(dbContext);

        var result = await service.SetActiveAsync(national.Id, false, null);

        Assert.False(result.Succeeded);
        Assert.True((await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == national.Id)).IsActive);
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateChapterLocation()
    {
        await using var dbContext = CreateDbContext();
        var national = AddOrganization(dbContext, "National", "NAT", OrganizationLevel.National, null);
        var state = AddOrganization(dbContext, "Florida", "FLA", OrganizationLevel.State, national.Id);
        var chapter = AddOrganization(dbContext, "Tampa", "FLA-7", OrganizationLevel.LocalChapter, state.Id);
        await dbContext.SaveChangesAsync();

        var service = new OrganizationAdminService(dbContext);

        var result = await service.UpdateAsync(chapter.Id, new OrganizationEditModel
        {
            Id = chapter.Id,
            Name = "Tampa",
            Abbreviation = "FLA-7",
            Level = OrganizationLevel.LocalChapter,
            ParentOrganizationUnitId = state.Id,
            City = "Pinellas Park",
            StateCode = "FL",
            IsActive = true
        }, null);

        Assert.True(result.Succeeded);
        var updated = await dbContext.OrganizationUnits.SingleAsync(unit => unit.Id == chapter.Id);
        Assert.Equal("Pinellas Park", updated.City);
        Assert.Equal("FL", updated.StateCode);
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
        Guid? parentId)
    {
        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = abbreviation,
            Level = level,
            ParentOrganizationUnitId = parentId,
            IsActive = true
        };

        dbContext.OrganizationUnits.Add(organization);
        return organization;
    }
}

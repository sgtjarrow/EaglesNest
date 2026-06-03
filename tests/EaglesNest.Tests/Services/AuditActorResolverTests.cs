using System.Security.Claims;
using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Tests.Services;

public class AuditActorResolverTests
{
    [Fact]
    public async Task ResolveMemberActorAsync_UsesLinkedMemberRoadName()
    {
        var factory = CreateDatabase();
        await using var dbContext = factory.CreateDbContext();
        var national = AddOrganization(dbContext);
        dbContext.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "login-1",
            FirstName = "Robert",
            LastName = "Johnson",
            RoadName = "Wizard",
            PrimaryChapterId = national.Id,
            Status = MemberStatus.PatchHolder
        });
        await dbContext.SaveChangesAsync();
        var resolver = new AuditActorResolver(factory);

        var actor = await resolver.ResolveMemberActorAsync(User("login-1", "super_eagle"));

        Assert.Equal("login-1", actor.ApplicationUserId);
        Assert.Equal("Wizard", actor.ActorName);
        Assert.Equal("User", actor.ActorSource);
    }

    [Fact]
    public async Task ResolveMemberActorAsync_FallsBackToAccountNameWhenUnlinked()
    {
        var factory = CreateDatabase();
        var resolver = new AuditActorResolver(factory);

        var actor = await resolver.ResolveMemberActorAsync(User("login-2", "account_user"));

        Assert.Equal("login-2", actor.ApplicationUserId);
        Assert.Equal("account_user", actor.ActorName);
    }

    private static ClaimsPrincipal User(string userId, string name)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, name)
            ],
            "Test"));
    }

    private static TestDbContextFactory CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    private static OrganizationUnit AddOrganization(ApplicationDbContext dbContext)
    {
        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = "National",
            Abbreviation = "NAT",
            Level = OrganizationLevel.National,
            Status = OrganizationStatus.Open
        };

        dbContext.OrganizationUnits.Add(organization);
        return organization;
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(options);
        }
    }
}

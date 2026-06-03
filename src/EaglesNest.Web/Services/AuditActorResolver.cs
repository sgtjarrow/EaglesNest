using System.Security.Claims;
using EaglesNest.Web.Data;
using EaglesNest.Web.Services.Chapters;
using EaglesNest.Web.Services.Members;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services;

public class AuditActorResolver(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<MemberActor> ResolveMemberActorAsync(ClaimsPrincipal user)
    {
        var (userId, actorName) = await ResolveAsync(user);
        return new MemberActor(userId, actorName, "User");
    }

    public async Task<ChapterActor> ResolveChapterActorAsync(ClaimsPrincipal user)
    {
        var (userId, actorName) = await ResolveAsync(user);
        return new ChapterActor(userId, actorName, "User");
    }

    private async Task<(string? UserId, string ActorName)> ResolveAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var accountName = user.Identity?.Name ?? user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, accountName ?? "Unknown User");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var member = await dbContext.Members
            .AsNoTracking()
            .Where(existing => existing.ApplicationUserId == userId)
            .Select(existing => new
            {
                existing.FirstName,
                existing.LastName,
                existing.PreferredName,
                existing.RoadName
            })
            .SingleOrDefaultAsync();

        if (member is not null)
        {
            var displayName = MemberAdminService.DisplayName(
                member.FirstName,
                member.LastName,
                member.PreferredName,
                member.RoadName);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return (userId, displayName);
            }
        }

        return (userId, accountName ?? "Unknown User");
    }
}

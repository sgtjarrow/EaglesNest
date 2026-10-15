using System.Text.Json;
using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Common;
using EaglesNest.Domain.Auditing;
using EaglesNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Infrastructure.Services;

public sealed class AuditEventWriter(ApplicationDbContext dbContext) : IAuditEventWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(string? actorUserId, string actorName, string action, object? details = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        var safeDetails = details is null ? "{}" : JsonSerializer.Serialize(details, JsonOptions);
        dbContext.AuditEvents.Add(new AuditEvent(actorUserId, actorName, action, safeDetails, notes));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AuditQueryService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IAuditQueryService
{
    public async Task<IReadOnlyList<AuditEventSummary>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEvents.AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(Math.Clamp(count, 1, 500))
            .Select(x => new AuditEventSummary(x.Id, x.ActorName, x.Action, x.OccurredAtUtc, x.DetailsJson, x.Notes))
            .ToListAsync(cancellationToken);
    }
}

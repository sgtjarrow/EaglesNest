using EaglesNest.Application.Common;
using EaglesNest.Application.Security;
using EaglesNest.Domain.Accounts;
using EaglesNest.Domain.Auditing;
using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void New_account_is_pending_and_not_deleted()
    {
        var user = new ApplicationUser();

        Assert.Equal(AccountApprovalStatus.Pending, user.ApprovalStatus);
        Assert.False(user.IsDeleted);
        Assert.False(user.IsManuallyLocked);
    }

    [Fact]
    public void Permission_catalog_contains_unique_permissions()
    {
        Assert.NotEmpty(Permissions.All);
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(Permissions.ManageUsers, Permissions.All);
        Assert.Contains(Permissions.ViewAuditLog, Permissions.All);
    }

    [Fact]
    public void Operation_result_preserves_failure_reason()
    {
        var result = OperationResult.Failure("Not allowed");

        Assert.False(result.Succeeded);
        Assert.Equal("Not allowed", result.Error);
    }

    [Fact]
    public async Task Audit_events_are_append_only()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var auditEvent = new AuditEvent("actor-1", "Administrator", AuditActions.UserCreated, "{}");
        db.AuditEvents.Add(auditEvent);
        await db.SaveChangesAsync();

        db.Entry(auditEvent).Property(nameof(AuditEvent.Notes)).CurrentValue = "changed";

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}

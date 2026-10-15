using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Identity;
using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Security;
using EaglesNest.Domain.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;

namespace EaglesNest.IntegrationTests;

public sealed class WebAndSqlIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString = CreateIsolatedConnectionString();
    private string? _previousConnectionString;
    private WebApplicationFactory<Program>? _factory;

    public Task InitializeAsync()
    {
        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
            });
        });
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Sign_in_page_renders_the_branded_access_flow()
    {
        using var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Eagles Nest", html);
        Assert.Contains("Request access", html);
        Assert.Contains("Army theme", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sql_server_enforces_normalized_email_uniqueness()
    {
        _ = _factory!.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"TEST-{suffix}@EXAMPLE.COM";
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"first-{suffix}",
            NormalizedUserName = $"FIRST-{suffix}".ToUpperInvariant(),
            Email = email.ToLowerInvariant(),
            NormalizedEmail = email
        });
        await db.SaveChangesAsync();
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"second-{suffix}",
            NormalizedUserName = $"SECOND-{suffix}".ToUpperInvariant(),
            Email = email.ToLowerInvariant(),
            NormalizedEmail = email
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Approval_permissions_and_account_safeguards_work_together()
    {
        _ = _factory!.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var approval = scope.ServiceProvider.GetRequiredService<IUserApprovalService>();
        var administration = scope.ServiceProvider.GetRequiredService<IUserAdministrationService>();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionAuthorizationService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var admin = new ApplicationUser
        {
            UserName = $"admin-{suffix}",
            Email = $"admin-{suffix}@example.com",
            EmailConfirmed = true,
            ApprovalStatus = AccountApprovalStatus.Approved,
            LockoutEnabled = true
        };
        Assert.True((await users.CreateAsync(admin, "IntegrationPass123")).Succeeded);
        Assert.True((await users.AddToRolesAsync(admin, [SystemRoles.Admin, SystemRoles.User])).Succeeded);

        var pending = new ApplicationUser
        {
            UserName = $"pending-{suffix}",
            Email = $"pending-{suffix}@example.com",
            EmailConfirmed = true,
            ApprovalStatus = AccountApprovalStatus.Pending,
            LockoutEnabled = true
        };
        Assert.True((await users.CreateAsync(pending, "IntegrationPass123")).Succeeded);

        var approved = await approval.ApproveAsync(pending.Id, admin.Id, admin.UserName!, "integration test");
        Assert.True(approved.Succeeded, approved.Error);
        Assert.Equal(AccountApprovalStatus.Approved, (await users.FindByIdAsync(pending.Id))!.ApprovalStatus);
        Assert.True(await permissions.HasPermissionAsync(admin.Id, Permissions.ManageUsers));

        var resetToken = await users.GeneratePasswordResetTokenAsync(pending);
        Assert.True((await users.ResetPasswordAsync(pending, resetToken, "ReplacementPass123")).Succeeded);
        Assert.False((await users.ResetPasswordAsync(pending, resetToken, "AnotherPass1234")).Succeeded);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await users.AccessFailedAsync(pending);
        }
        Assert.True(await users.IsLockedOutAsync(pending));
        var unlocked = await administration.SetManualLockAsync(pending.Id, false, admin.Id, admin.UserName!);
        Assert.True(unlocked.Succeeded, unlocked.Error);
        Assert.False(await users.IsLockedOutAsync(pending));

        var selfLock = await administration.SetManualLockAsync(admin.Id, true, admin.Id, admin.UserName!);
        Assert.False(selfLock.Succeeded);
        Assert.True(await users.IsInRoleAsync(admin, SystemRoles.Admin));
        var activeAdmins = (await users.GetUsersInRoleAsync(SystemRoles.Admin))
            .Count(x => !x.IsDeleted && !x.IsManuallyLocked && x.ApprovalStatus == AccountApprovalStatus.Approved);
        Assert.Equal(1, activeAdmins);
        var finalAdminLock = await administration.SetManualLockAsync(admin.Id, true, pending.Id, pending.UserName!);
        Assert.False(finalAdminLock.Succeeded);

        var deleted = await administration.SoftDeleteAsync(pending.Id, admin.Id, admin.UserName!);
        Assert.True(deleted.Succeeded, deleted.Error);
        Assert.True((await users.FindByIdAsync(pending.Id))!.IsDeleted);
        Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == AuditActions.UserApproved));
        Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == AuditActions.UserDeleted));
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _previousConnectionString);
    }

    private static string CreateIsolatedConnectionString()
    {
        var baseConnection = Environment.GetEnvironmentVariable("EAGLESNEST_TEST_CONNECTION")
            ?? "Server=localhost;Database=EaglesNest_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True";
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"EaglesNest_IntegrationTests_{Guid.NewGuid():N}"
        };
        return builder.ConnectionString;
    }
}

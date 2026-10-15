using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Security;
using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EaglesNest.IntegrationTests;

public sealed class BootstrapAdminIntegrationTests
{
    private const string TestUserName = "bootstrap-admin";
    private const string TestEmail = "bootstrap-admin@example.com";
    private const string TestPassword = "BootstrapPass123";

    [Fact]
    public async Task Missing_bootstrap_secrets_seed_security_data_without_creating_an_admin()
    {
        var connectionString = CreateIsolatedConnectionString();
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        var factory = CreateFactory(connectionString, new Dictionary<string, string?>
        {
            ["BootstrapAdmin:UserName"] = string.Empty,
            ["BootstrapAdmin:Email"] = string.Empty,
            ["BootstrapAdmin:Password"] = string.Empty
        });

        try
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Empty(await db.Users.ToListAsync());
            Assert.Equal(2, await db.Roles.CountAsync());
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Action == AuditActions.BootstrapAdminCreated));
        }
        finally
        {
            await factory.DisposeAsync();
            await DeleteDatabaseAsync(connectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnectionString);
        }
    }

    [Fact]
    public async Task Bootstrap_can_retain_the_password_and_is_idempotent_across_restarts()
    {
        var connectionString = CreateIsolatedConnectionString();
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        var configuration = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:UserName"] = TestUserName,
            ["BootstrapAdmin:Email"] = TestEmail,
            ["BootstrapAdmin:Password"] = TestPassword,
            ["BootstrapAdmin:MustChangePassword"] = "false"
        };

        try
        {
            await using (var firstFactory = CreateFactory(connectionString, configuration))
            {
                await AssertBootstrapStateAsync(firstFactory.Services);
            }

            await using (var restartedFactory = CreateFactory(connectionString, configuration))
            {
                await AssertBootstrapStateAsync(restartedFactory.Services);
            }
        }
        finally
        {
            await DeleteDatabaseAsync(connectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnectionString);
        }
    }

    private static async Task AssertBootstrapStateAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionAuthorizationService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var admin = await users.FindByNameAsync(TestUserName);
        Assert.NotNull(admin);
        Assert.True(admin.EmailConfirmed);
        Assert.False(admin.MustChangePassword);
        Assert.True(await users.CheckPasswordAsync(admin, TestPassword));
        Assert.True(await users.IsInRoleAsync(admin, SystemRoles.Admin));
        Assert.True(await users.IsInRoleAsync(admin, SystemRoles.User));
        Assert.True(await permissions.HasPermissionAsync(admin.Id, Permissions.ManageUsers));

        Assert.Equal(1, await db.Users.CountAsync());
        var audit = Assert.Single(await db.AuditEvents
            .Where(x => x.Action == AuditActions.BootstrapAdminCreated)
            .ToListAsync());
        Assert.DoesNotContain(TestPassword, audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain(TestPassword, audit.Notes ?? string.Empty, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string connectionString,
        IReadOnlyDictionary<string, string?> bootstrapConfiguration)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>(bootstrapConfiguration)
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                };
                configuration.AddInMemoryCollection(values);
            });
        });
    }

    private static async Task DeleteDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    private static string CreateIsolatedConnectionString()
    {
        var baseConnection = Environment.GetEnvironmentVariable("EAGLESNEST_TEST_CONNECTION")
            ?? "Server=localhost;Database=EaglesNest_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True";
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"EaglesNest_BootstrapTests_{Guid.NewGuid():N}"
        };
        return builder.ConnectionString;
    }
}

using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Security;
using EaglesNest.Domain.Accounts;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EaglesNest.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { SystemRoles.Admin, SystemRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"create role {roleName}");
            }
        }

        var adminRole = await roleManager.FindByNameAsync(SystemRoles.Admin)
            ?? throw new InvalidOperationException("Admin role could not be loaded.");
        var userRole = await roleManager.FindByNameAsync(SystemRoles.User)
            ?? throw new InvalidOperationException("User role could not be loaded.");

        await SeedPermissionsAsync(db, adminRole.Id, Permissions.All, cancellationToken);
        await SeedPermissionsAsync(db, userRole.Id, [Permissions.ViewDashboard], cancellationToken);

        await BootstrapAdminAsync(scope.ServiceProvider, configuration, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, string roleId, IEnumerable<string> permissions, CancellationToken cancellationToken)
    {
        var existing = await db.RolePermissions.Where(x => x.RoleId == roleId).Select(x => x.Permission).ToListAsync(cancellationToken);
        foreach (var permission in permissions.Except(existing, StringComparer.Ordinal))
        {
            db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = roleId, Permission = permission });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        if ((await userManager.GetUsersInRoleAsync(SystemRoles.Admin)).Any(x => !x.IsDeleted)) return;

        var userName = configuration["BootstrapAdmin:UserName"];
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No Admin account exists. Configure BootstrapAdmin:UserName, Email, and Password using user-secrets or deployment secrets.");
            return;
        }
        var mustChangePassword = configuration.GetValue("BootstrapAdmin:MustChangePassword", true);

        var user = new ApplicationUser
        {
            UserName = userName.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            ApprovalStatus = AccountApprovalStatus.Approved,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            ApprovedByUserId = "SYSTEM",
            MustChangePassword = mustChangePassword,
            LockoutEnabled = true
        };

        var create = await userManager.CreateAsync(user, password);
        EnsureSucceeded(create, "create the bootstrap Admin");
        EnsureSucceeded(await userManager.AddToRolesAsync(user, [SystemRoles.Admin, SystemRoles.User]), "assign bootstrap Admin roles");

        var audit = services.GetRequiredService<IAuditEventWriter>();
        await audit.WriteAsync(null, "SYSTEM", AuditActions.BootstrapAdminCreated,
            new { TargetUserId = user.Id, user.UserName, user.Email }, cancellationToken: cancellationToken);
        logger.LogInformation(
            mustChangePassword
                ? "Bootstrap Admin {UserName} was created. Remove bootstrap secrets and change the password at first sign-in."
                : "Bootstrap Admin {UserName} was created. Remove bootstrap secrets.",
            user.UserName);
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {operation}: {string.Join(" ", result.Errors.Select(x => x.Description))}");
        }
    }
}

using EaglesNest.Application.Abstractions;
using EaglesNest.Application.Security;
using EaglesNest.Infrastructure.Data;
using EaglesNest.Infrastructure.Security;
using EaglesNest.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EaglesNest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEaglesNestInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IAuditEventWriter, AuditEventWriter>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IUserApprovalService, UserApprovalService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<DevelopmentEmailSender>();
        services.AddSingleton<IApplicationEmailSender>(sp => sp.GetRequiredService<DevelopmentEmailSender>());
        services.AddSingleton<IDevelopmentEmailOutbox>(sp => sp.GetRequiredService<DevelopmentEmailSender>());

        return services;
    }

    public static void AddEaglesNestPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions.All)
        {
            options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
        }
    }
}

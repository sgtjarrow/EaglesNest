using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EaglesNest.Web.Security;

public sealed class ForcedPasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPrefixes =
    [
        "/Account/Manage/ChangePassword",
        "/Account/Logout",
        "/_framework",
        "/_blazor",
        "/fonts",
        "/lib"
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true && !IsAllowedPath(context.Request.Path))
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user?.MustChangePassword == true)
            {
                context.Response.Redirect("/Account/Manage/ChangePassword?forced=true");
                return;
            }
        }

        await next(context);
    }

    private static bool IsAllowedPath(PathString path) =>
        AllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)) ||
        path.Value?.EndsWith(".css", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".js", StringComparison.OrdinalIgnoreCase) == true ||
        path.Value?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true;
}

using EaglesNest.Domain.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EaglesNest.Infrastructure.Identity;

public sealed class EaglesNestSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (user.IsDeleted || user.IsManuallyLocked || user.ApprovalStatus != AccountApprovalStatus.Approved)
        {
            return Task.FromResult(false);
        }

        return base.CanSignInAsync(user);
    }
}

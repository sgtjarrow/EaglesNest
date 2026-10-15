using EaglesNest.Application.Abstractions;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EaglesNest.Web.Components.Account;

internal sealed class IdentityEmailSenderAdapter(IApplicationEmailSender emailSender) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        emailSender.SendPasswordResetAsync(email, resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Task.CompletedTask;
}

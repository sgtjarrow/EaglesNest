using Microsoft.AspNetCore.Identity;

namespace EaglesNest.Web.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsLoginDisabled { get; set; }
    public string? LoginDisabledReason { get; set; }
}


using System.Security.Claims;
using CampusX.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CampusX.Services;

// Retains the current GUID, display-name and Student/Admin claims without duplicating enum roles in tables.
public class CampusXUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public CampusXUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options) : base(userManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var name = identity.FindFirst(Options.ClaimsIdentity.UserNameClaimType);
        if (name is not null)
            identity.RemoveClaim(name);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, user.FullName));
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, user.Role.ToString()));
        return identity;
    }
}

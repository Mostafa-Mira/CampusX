using Microsoft.AspNetCore.Authorization;

namespace CampusX.Services;

public sealed class MarketplaceAccessHandler(MarketplaceAccessEvaluator access)
    : AuthorizationHandler<MarketplaceAccessRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        MarketplaceAccessRequirement requirement)
    {
        var (status, role) = await access.GetAccessAsync(context.User);
        if ((status is MarketplaceAccessStatus.Eligible or MarketplaceAccessStatus.DevelopmentTester) &&
            (requirement.RequiredRole is null || requirement.RequiredRole == role))
            context.Succeed(requirement);
    }
}

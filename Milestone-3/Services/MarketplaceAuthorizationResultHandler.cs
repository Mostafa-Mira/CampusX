using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CampusX.Services;

public sealed class MarketplaceAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Forbidden && policy.Requirements.OfType<MarketplaceAccessRequirement>().Any())
        {
            var access = context.RequestServices.GetRequiredService<MarketplaceAccessHandler>();
            var status = await access.GetStatusAsync(context.User);
            if (status == MarketplaceAccessStatus.PendingVerification)
            {
                context.Response.Redirect($"{context.Request.PathBase}/Account/PendingUniversityVerification");
                return;
            }
            if (status is MarketplaceAccessStatus.Blocked or MarketplaceAccessStatus.MissingAccount)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Your account cannot access the marketplace.");
                return;
            }
        }
        await _defaultHandler.HandleAsync(next, context, policy, result);
    }
}

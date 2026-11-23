using System.Security.Claims;
using CampusX.Models;
using Microsoft.AspNetCore.Authorization;

namespace CampusX.Services;

internal sealed class ProductOwnershipRequirement : IAuthorizationRequirement;

internal sealed class ProductOwnershipHandler : AuthorizationHandler<ProductOwnershipRequirement, Product>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ProductOwnershipRequirement requirement, Product product)
    {
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) &&
            product.IsOwnedBy(userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

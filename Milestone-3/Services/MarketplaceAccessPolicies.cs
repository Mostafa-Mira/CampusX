using CampusX.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Identity;

namespace CampusX.Services;

public static class MarketplaceAccessPolicies
{
    public const string Marketplace = "Marketplace";
    public const string AuthenticatedOnly = "AuthenticatedOnly";
    public const string Student = nameof(UserRole.Student);
    public const string Admin = nameof(UserRole.Admin);
    public const string ProductOwner = "ProductOwner";

    internal static void AddCampusXAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            var marketplace = CreateMarketplacePolicy();
            // Protect both plain [Authorize] actions and routes without an explicit policy.
            options.DefaultPolicy = marketplace;
            options.FallbackPolicy = marketplace;
            options.AddPolicy(Marketplace, marketplace);
            options.AddPolicy(Student, CreateMarketplacePolicy(UserRole.Student));
            options.AddPolicy(Admin, CreateMarketplacePolicy(UserRole.Admin));
            options.AddPolicy(ProductOwner, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new ProductOwnershipRequirement()));
            options.AddPolicy(AuthenticatedOnly, policy =>
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser());
        });
        services.AddHttpContextAccessor();
        services.AddScoped<DevelopmentTesterAccess>();
        services.AddScoped<MarketplaceAccessEvaluator>();
        services.AddScoped<IAuthorizationHandler, MarketplaceAccessHandler>();
        services.AddSingleton<IAuthorizationHandler, ProductOwnershipHandler>();
        services.AddScoped<IAuthorizationMiddlewareResultHandler, MarketplaceAuthorizationResultHandler>();
    }

    private static AuthorizationPolicy CreateMarketplacePolicy(UserRole? requiredRole = null) =>
        new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme)
            .RequireAuthenticatedUser().AddRequirements(new MarketplaceAccessRequirement(requiredRole)).Build();
}

public sealed record MarketplaceAccessRequirement(UserRole? RequiredRole = null) : IAuthorizationRequirement;

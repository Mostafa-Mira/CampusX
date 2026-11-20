using System.Security.Claims;
using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Services;

public static class MarketplaceAccessPolicies
{
    public const string Marketplace = "Marketplace";
    public const string AuthenticatedOnly = "AuthenticatedOnly";
    public const string Student = nameof(UserRole.Student);
    public const string Admin = nameof(UserRole.Admin);
}

public sealed record MarketplaceAccessRequirement(UserRole? RequiredRole = null) : IAuthorizationRequirement;

public enum MarketplaceAccessStatus { Guest, MissingAccount, Blocked, PendingVerification, Eligible }

public sealed class MarketplaceAccessHandler : AuthorizationHandler<MarketplaceAccessRequirement>
{
    private readonly CampusXDbContext _context;

    public MarketplaceAccessHandler(CampusXDbContext context) => _context = context;

    public async Task<MarketplaceAccessStatus> GetStatusAsync(ClaimsPrincipal principal) =>
        (await GetAccessAsync(principal)).Status;

    internal async Task<MarketplaceAccessStatus> GetStatusAsync(Guid id) =>
        (await GetAccessAsync(id)).Status;

    private async Task<(MarketplaceAccessStatus Status, UserRole? Role)> GetAccessAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return (MarketplaceAccessStatus.Guest, null);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return (MarketplaceAccessStatus.MissingAccount, null);

        return await GetAccessAsync(id);
    }

    private async Task<(MarketplaceAccessStatus Status, UserRole? Role)> GetAccessAsync(Guid id)
    {
        // The database supplies the current role and eligibility on each authorization check.
        var user = await _context.ApplicationUsers.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new { user.Role, user.IsBlocked, user.UniversityEmail, user.UniversityEmailConfirmed, user.VerifiedUniversityEmail })
            .SingleOrDefaultAsync();

        if (user is null)
            return (MarketplaceAccessStatus.MissingAccount, null);
        if (user.IsBlocked)
            return (MarketplaceAccessStatus.Blocked, null);
        return StudentUniversities.HasVerifiedEmail(user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed)
            ? (MarketplaceAccessStatus.Eligible, user.Role)
            : (MarketplaceAccessStatus.PendingVerification, null);
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        MarketplaceAccessRequirement requirement)
    {
        var (status, role) = await GetAccessAsync(context.User);
        if (status == MarketplaceAccessStatus.Eligible &&
            (requirement.RequiredRole is null || requirement.RequiredRole == role))
            context.Succeed(requirement);
    }

    public static AuthorizationPolicy CreateMarketplacePolicy(UserRole? requiredRole = null) =>
        new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme)
            .RequireAuthenticatedUser().AddRequirements(new MarketplaceAccessRequirement(requiredRole)).Build();
}

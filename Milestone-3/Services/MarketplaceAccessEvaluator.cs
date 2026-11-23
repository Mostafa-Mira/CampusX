using System.Security.Claims;
using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Services;

public enum MarketplaceAccessStatus { Guest, MissingAccount, Blocked, PendingVerification, Eligible, DevelopmentTester }

public sealed class MarketplaceAccessEvaluator(CampusXDbContext context, DevelopmentTesterAccess testerAccess)
{
    public async Task<MarketplaceAccessStatus> GetStatusAsync(ClaimsPrincipal principal) =>
        (await GetAccessAsync(principal)).Status;

    internal async Task<MarketplaceAccessStatus> GetStatusAsync(Guid id) =>
        (await GetAccessAsync(id)).Status;

    internal async Task<(MarketplaceAccessStatus Status, UserRole? Role)> GetAccessAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return (MarketplaceAccessStatus.Guest, null);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return (MarketplaceAccessStatus.MissingAccount, null);

        return await GetAccessAsync(id);
    }

    private async Task<(MarketplaceAccessStatus Status, UserRole? Role)> GetAccessAsync(Guid id)
    {
        // Use the cookie's user ID to read the current role and eligibility from the database.
        var user = await context.ApplicationUsers.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new { user.Role, user.IsBlocked, user.Email, user.UniversityEmail, user.UniversityEmailConfirmed, user.VerifiedUniversityEmail })
            .SingleOrDefaultAsync();

        if (user is null)
            return (MarketplaceAccessStatus.MissingAccount, null);
        if (user.IsBlocked)
            return (MarketplaceAccessStatus.Blocked, null);
        if (StudentUniversities.HasVerifiedEmail(user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed))
            return (MarketplaceAccessStatus.Eligible, user.Role);
        if (await testerAccess.IsAllowedAsync(id, user.Role, user.Email))
            return (MarketplaceAccessStatus.DevelopmentTester, user.Role);
        return (MarketplaceAccessStatus.PendingVerification, null);
    }
}

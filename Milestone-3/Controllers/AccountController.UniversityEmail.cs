using CampusX.Services;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

public partial class AccountController
{
    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpGet]
    public Task<IActionResult> PendingUniversityVerification() => VerificationPageAsync();

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpGet]
    public Task<IActionResult> UniversityEmail() => VerificationPageAsync(allowConfirmed: true);

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendUniversityVerificationCode()
    {
        return await RunVerificationAsync(() => _verification.SendAsync(CurrentUserId));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyUniversityEmail(UniversityVerificationViewModel model)
    {
        // Invalid submissions count toward the persisted budget; never echo the submitted code.
        var code = model.Code;
        ModelState.Clear();
        return await RunVerificationAsync(() => _verification.VerifyAsync(CurrentUserId, code), enterMarketplace: true);
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUniversityEmail(ChangeUniversityEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return await VerificationPageAsync(allowConfirmed: true);
        return await RunVerificationAsync(() => _verification.ChangeEmailAsync(CurrentUserId, model.UniversityEmail));
    }

    private async Task<IActionResult> RunVerificationAsync(Func<Task<(bool Success, string Message)>> operation,
        bool enterMarketplace = false)
    {
        try
        {
            var result = await operation();
            if (result.Success)
            {
                TempData["AccountMessage"] = result.Message;
                return enterMarketplace ? RedirectToAction("Index", "Home") : RedirectToAction(nameof(UniversityEmail));
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number == 51020)
        {
            ModelState.AddModelError(string.Empty, "Verification is busy. Please try again.");
        }
        return await VerificationPageAsync(allowConfirmed: true);
    }

    private async Task<IActionResult> VerificationPageAsync(bool allowConfirmed = false)
    {
        try
        {
            var model = await _verification.GetPageAsync(CurrentUserId);
            if (model.IsConfirmed && !allowConfirmed)
                return RedirectToAction("Index", "Home");
            return View("PendingUniversityVerification", model);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
    }
}
